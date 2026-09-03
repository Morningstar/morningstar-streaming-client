# Sequence Integrity: How Gaps (Missing Messages) Work

This document explains, in plain terms, how the streaming client's
`SequenceGapDetector` handles **forward gaps** — situations where the sequence
number jumps ahead, implying one or more messages were skipped — and, in
particular, how it behaves when the jump is **very large**.

> Source: `Morningstar.Streaming.Client/Services/Telemetry/SequenceGapDetector.cs`
> (`Process` forward-gap branch + `AdvanceFloor`) and the counter mapping in the
> implementation of the ISequenceLogger.cs
>
> For the companion write-up on duplicates, see
> [`sequence-duplicate-detection.md`](./sequence-duplicate-detection.md).

## The mental model

Every message stream — tracked separately per **(PerformanceId, event type)** —
is like **numbered tickets** arriving at a door: 1, 2, 3, 4… The detector
remembers three things per stream:

- **High-water mark (HWM):** the highest ticket number seen so far.
- **Floor (low-water mark):** the bottom of a sliding window (HWM minus the
  window size, default `1000`). Anything at or below the floor is _too old to
  remember_.
- **Missing set:** ticket numbers below the HWM that we _expected but haven't
  seen yet_ (the outstanding gaps).

## What counts as a gap

When a ticket arrives with a number **higher than the next expected one**
(`seq > HWM + 1`), everything between the last seen number and the new one is
treated as **missing**:

```
gapStart     = HWM + 1
gapEnd       = seq - 1
missingCount = gapEnd - gapStart + 1     ← reported to the metric
```

`missingCount` is added to `messages_missing_total` (missing counts by
gap **size**, not by occurrence).

## The memory-bounding rule

A naive detector would remember _every_ skipped number so it could later mark
late arrivals as **Recovered**. That is unbounded: one huge jump could store
millions of entries. Instead, the detector only retains the gaps that fall
**inside the window** below the new HWM. Before recording the gap it advances
the floor:

```
AdvanceFloor(seq - window)               → Floor becomes seq - window
addFrom = max(gapStart, Floor + 1)       → only the in-window tail is stored
store addFrom … gapEnd in the Missing set
```

So the Missing set can **never exceed the window size** (~1000 entries per key),
no matter how large the jump. Gaps below the new floor are counted in the
reported `missingCount` but are **never stored** — they are effectively
expired-on-arrival.

## Worked example: a very large jump

**Scenario:** the stream runs in order from `1` up to `2000`, then the next
message received is `10000`. (Window = 1000.)

### State before the jump (after 1…2000 in order)

| Quantity          | Value                          |
| ----------------- | ------------------------------ |
| High-water mark   | `2000`                         |
| Floor (low-water) | `1000` (always `HWM − window`) |
| Missing set       | empty                          |

### Processing `10000` (forward-gap branch)

```
gapStart     = 2000 + 1   = 2001
gapEnd       = 10000 - 1  = 9999
missingCount = 9999 - 2001 + 1 = 7999          ← reported to the metric

AdvanceFloor(10000 - 1000 = 9000)              → Floor moves 1000 → 9000
addFrom = max(2001, 9000 + 1) = 9001
store 9001 … 9999                              → 999 entries
HighWaterMark = 10000
```

### State after the jump

| Quantity                                 | Value                  |
| ---------------------------------------- | ---------------------- |
| High-water mark                          | `10000`                |
| Floor (low-water)                        | `9000`                 |
| Entries added to the Missing set         | **999** (`9001…9999`)  |
| Missing count **reported to the metric** | **7999** (`2001…9999`) |

### The key insight

| Quantity                                    | Value    | Meaning                                    |
| ------------------------------------------- | -------- | ------------------------------------------ |
| Reported missing (`messages_missing_total`) | **7999** | Every sequence we believe we skipped       |
| Missing entries actually retained in memory | **999**  | Only those inside the window (`9001…9999`) |
| Silently dropped (`2001…9000`)              | 7000     | Below the new floor — never stored         |

This is the **memory-bounding guarantee**: even an enormous jump cannot make the
Missing set grow beyond the window size, so it can't blow up memory.

## Consequences for recovery

- If any of `9001…9999` arrive later, they are classified **Recovered** and
  removed from the Missing set.
- If any of `2001…9000` arrive later, they are classified **Expired** (they are
  `≤ Floor`) — the detector has already discarded them and can no longer tell a
  late original from a duplicate.

## Caveat: gap vs. feed restart / rollover

This logic assumes a large jump is a _genuine_ gap (messages really were
skipped). If instead the producer **restarts or rolls over** its numbering, the
detector still treats the jump as "7999 skipped." That is the intended behavior
here, but it's worth remembering when interpreting a sudden large spike in
`messages_missing_total` — a restart can look identical to a real gap.
