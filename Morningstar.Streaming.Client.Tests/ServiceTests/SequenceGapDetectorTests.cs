using FluentAssertions;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
    public class SequenceGapDetectorTests
    {
        private const string PerfId = "0P0000TEST";
        private const string EventType = "Trade";

        private readonly Guid subscriptionId = Guid.NewGuid();
        private readonly RecordingSequenceLogger recorder = new();

        private SequenceGapDetector CreateDetector(int window = SequenceGapDetector.DefaultWindow, long? staleThresholdMs = null)
            => new(subscriptionId, recorder, window, staleThresholdMs);

        [Fact]
        public void FirstMessage_ForKey_IsInOrderBaseline()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 100);

            recorder.Records.Should().ContainSingle();
            recorder.LastFlags.Should().Be(SequenceFlags.InOrder);
        }

        [Fact]
        public void InOrderStream_ReportsNoAnomalies()
        {
            var detector = CreateDetector();

            for (long seq = 1; seq <= 5; seq++)
            {
                detector.Process(PerfId, EventType, seq);
            }

            recorder.Records.Should().HaveCount(5);
            recorder.Records.Should().OnlyContain(r => r.Flags == SequenceFlags.InOrder && r.MissingCount == 0);
        }

        [Fact]
        public void ForwardGap_ReportsMissingWithGapSize()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 2);
            detector.Process(PerfId, EventType, 5); // 3 and 4 missing

            recorder.LastFlags.Should().Be(SequenceFlags.Missing);
            recorder.LastMissing.Should().Be(2);
        }

        [Fact]
        public void DuplicateOfHighWaterMark_IsDuplicate()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 2);
            detector.Process(PerfId, EventType, 2); // re-receipt of the top

            recorder.LastFlags.Should().Be(SequenceFlags.Duplicate);
        }

        [Fact]
        public void LateArrivalAlreadySeen_IsOutOfOrderAndDuplicate()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 2);
            detector.Process(PerfId, EventType, 3);
            detector.Process(PerfId, EventType, 2); // below hwm, in-window, already seen

            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Duplicate);
        }

        [Fact]
        public void LateArrivalFillingGap_IsOutOfOrderAndRecovered()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 2);
            detector.Process(PerfId, EventType, 5); // 3,4 missing

            detector.Process(PerfId, EventType, 3);
            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Recovered);

            detector.Process(PerfId, EventType, 4);
            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Recovered);
        }

        [Fact]
        public void ArrivalOlderThanWindow_IsOutOfOrderAndExpired()
        {
            var detector = CreateDetector(window: 2);

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 5); // floor advances to 3; 2 is below floor

            detector.Process(PerfId, EventType, 2); // 2 <= floor

            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Expired);
        }

        [Fact]
        public void GapAgingOutOfWindow_IsReportedExpired()
        {
            var detector = CreateDetector(window: 2);

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 3); // 2 missing, tracked
            detector.Process(PerfId, EventType, 4); // advances floor past 2 -> expired

            recorder.Records.Should().Contain(r => r.Flags == SequenceFlags.Expired && r.MissingCount == 1);
        }

        [Fact]
        public void LargeGap_IsBoundedToWindowButReportsFullMissingCount()
        {
            var detector = CreateDetector(window: 10);

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 1_000_000); // full gap 2..999999

            recorder.LastFlags.Should().Be(SequenceFlags.Missing);
            recorder.LastMissing.Should().Be(999_998);

            // Below the window floor (999990) -> expired, proving old gaps were not retained.
            detector.Process(PerfId, EventType, 5);
            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Expired);

            // Within the retained window tail -> recoverable.
            detector.Process(PerfId, EventType, 999_995);
            recorder.LastFlags.Should().Be(SequenceFlags.OutOfOrder | SequenceFlags.Recovered);
        }

        [Theory]
        [InlineData(null, EventType, 1L)]
        [InlineData("", EventType, 1L)]
        [InlineData(PerfId, null, 1L)]
        [InlineData(PerfId, "", 1L)]
        [InlineData(PerfId, EventType, null)]
        public void MissingKeyFields_AreUnclassified(string? perfId, string? eventType, long? seq)
        {
            var detector = CreateDetector();

            detector.Process(perfId, eventType, seq);

            recorder.Records.Should().ContainSingle();
            recorder.LastFlags.Should().Be(SequenceFlags.Unclassified);
        }

        [Fact]
        public void Keys_AreIsolatedByPerformanceIdAndEventType()
        {
            var detector = CreateDetector();

            detector.Process("A", EventType, 1);
            detector.Process("B", EventType, 1);
            detector.Process("A", "Close", 1);

            detector.Process("A", EventType, 3);   // gap on (A, Trade) only
            recorder.LastFlags.Should().Be(SequenceFlags.Missing);

            detector.Process("B", EventType, 2);   // unaffected
            recorder.LastFlags.Should().Be(SequenceFlags.InOrder);

            detector.Process("A", "Close", 2);      // unaffected
            recorder.LastFlags.Should().Be(SequenceFlags.InOrder);
        }

        [Fact]
        public void Prune_EvictsIdleKeys_AndFlushesOutstandingGapsAsExpired()
        {
            var detector = CreateDetector(staleThresholdMs: 1_000);

            detector.Process(PerfId, EventType, 1);
            detector.Process(PerfId, EventType, 3); // 2 outstanding
            detector.KeyCount.Should().Be(1);

            detector.Prune(Environment.TickCount64 + 60_000);

            detector.KeyCount.Should().Be(0);
            recorder.Records.Should().Contain(r => r.Flags == SequenceFlags.Expired && r.MissingCount == 1);
        }

        [Fact]
        public void Prune_KeepsActiveKeys()
        {
            var detector = CreateDetector(staleThresholdMs: 60_000);

            detector.Process(PerfId, EventType, 1);

            detector.Prune(Environment.TickCount64);

            detector.KeyCount.Should().Be(1);
        }

        [Fact]
        public void Clear_DropsAllState()
        {
            var detector = CreateDetector();

            detector.Process(PerfId, EventType, 1);
            detector.Process("B", EventType, 1);
            detector.KeyCount.Should().Be(2);

            detector.Clear();

            detector.KeyCount.Should().Be(0);
        }

        private sealed class RecordingSequenceLogger : ISequenceLogger
        {
            public readonly List<(SequenceFlags Flags, long MissingCount)> Records = new();

            public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose) { }

            public void UnregisterSubscription(Guid subscriptionId) { }

            public void Record(Guid subscriptionId, SequenceFlags flags, long missingCount)
                => Records.Add((flags, missingCount));

            public void Flush() { }

            public SequenceFlags LastFlags => Records[^1].Flags;

            public long LastMissing => Records[^1].MissingCount;
        }
    }
}
