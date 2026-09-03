# Sequence Integrity: How Duplicate Detection Works

This document explains, in plain terms, how the streaming client's
`SequenceGapDetector` decides that a message is a **duplicate**.

> Source: `Morningstar.Streaming.Client/Services/Telemetry/SequenceGapDetector.cs`
> (`Process`) and the counter mapping in the implementation of the ISequenceLogger.cs

## The mental model

Every message stream — tracked separately per **(PerformanceId, event type)** —
is like **numbered tickets** arriving at a door: 1, 2, 3, 4… The detector
remembers three things per stream:

- **High-water mark (HWM):** the highest ticket number seen so far.
- **Floor:** the bottom of a sliding window (HWM minus the window size, default
  `1000`). Anything at or below the floor is _too old to remember_.
- **Missing set:** ticket numbers below the HWM that we _expected but haven't
  seen yet_ (the outstanding gaps).

The core rule:

> **A ticket inside the window that is _not_ one of the outstanding gaps must be
> one we've already let in — so it's a duplicate.**

## The duplicate cases

A message is flagged `Duplicate` in two situations:

1. **Exact repeat of the newest** (`seq == HWM`): the same top ticket arrives
   again. An obvious duplicate. A third or fourth resend of that same number
   each add one more (they are all still `seq == HWM`).
2. **An older ticket we already have** (`seq < HWM`, above the floor, and _not_
   in the Missing set): it's within recent memory, and since it isn't a known
   gap, we must have seen it before → duplicate.

## How duplicates are told apart from look-alikes

When an _older_ ticket (`seq < HWM`) shows up, the detector asks three questions
**in order**:

| Question                                            | Verdict                     |
| --------------------------------------------------- | --------------------------- |
| Is it **at or below the floor**? (too old to judge) | **Expired**                 |
| Is it **filling a known gap** (in the Missing set)? | **Recovered** (gap removed) |
| Otherwise                                           | **Duplicate**               |

So "duplicate" is essentially the _default_ verdict for an in-window message
that we can neither treat as brand-new, nor as a legitimately missing ticket
arriving late.

## What happens to the metric

Each `Duplicate` flag bumps the `messages_duplicate_total` counter by
**1**. Duplicates count _occurrences_ — unlike `missing` / `missing_expired`,
which count by gap size.

## Worked example

Tickets arriving in this order: **1, 2, 4, 2, 3, 2**

| Arrival | HWM before | Verdict       | Why                                                        |
| ------- | ---------- | ------------- | ---------------------------------------------------------- |
| `1`     | —          | In-order      | First ticket seen; becomes the baseline (HWM = 1).         |
| `2`     | 1          | In-order      | Exactly HWM + 1; normal advance (HWM = 2).                 |
| `4`     | 2          | Missing (1)   | Forward gap: `3` is now expected-but-unseen (HWM = 4).     |
| `2`     | 4          | **Duplicate** | Below HWM, above floor, and `2` isn't a known gap.         |
| `3`     | 4          | Recovered     | Below HWM and `3` _is_ in the Missing set → gap filled.    |
| `2`     | 4          | **Duplicate** | Still below HWM, above floor, not a gap → duplicate again. |

## One subtlety worth knowing

A duplicate of a very old ticket that has already fallen **below the floor** is
reported as **Expired**, not Duplicate — once a number is out of the window the
detector can't distinguish an "old duplicate" from a "very late original." This
is a deliberate trade-off that keeps memory bounded (only the most recent
`window` sequences are retained).
