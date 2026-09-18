using Morningstar.Streaming.Client.Services.Telemetry;
using TechTalk.SpecFlow;

namespace Morningstar.Streaming.Client.BDD.Tests.StepDefinitions;

[Binding]
public class SequenceIntegrityStepDefinitions
{
    private const string DefaultPerformanceId = "0P0000TEST";
    private const string DefaultEventType = "Trade";

    private readonly Guid subscriptionId = Guid.NewGuid();
    private readonly RecordingSequenceLogger recorder = new();
    private SequenceGapDetector detector = null!;

    [Given(@"a sequence detector")]
    public void GivenASequenceDetector()
    {
        detector = new SequenceGapDetector(subscriptionId, recorder);
    }

    [Given(@"a sequence detector with a tracking window of (\d+) sequences")]
    public void GivenASequenceDetectorWithWindow(int window)
    {
        detector = new SequenceGapDetector(subscriptionId, recorder, window);
    }

    [When(@"messages arrive with sequence numbers (.*)")]
    public void WhenMessagesArriveWithSequenceNumbers(string sequenceNumbers)
    {
        foreach (var value in sequenceNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            detector.Process(DefaultPerformanceId, DefaultEventType, long.Parse(value));
        }
    }

    [When(@"a message arrives with no sequence number")]
    public void WhenAMessageArrivesWithNoSequenceNumber()
    {
        detector.Process(DefaultPerformanceId, DefaultEventType, null);
    }

    [When(@"a message arrives for instrument ""(.*)"" event ""(.*)"" with sequence (\d+)")]
    public void WhenAMessageArrivesForInstrument(string performanceId, string eventType, long sequenceNumber)
    {
        detector.Process(performanceId, eventType, sequenceNumber);
    }

    [Then(@"no sequence anomaly is recorded")]
    public void ThenNoSequenceAnomalyIsRecorded()
    {
        Assert.All(recorder.Records, record => Assert.Equal(SequenceFlags.InOrder, record.Flags));
    }

    [Then(@"an? ""(.*)"" anomaly is recorded")]
    public void ThenAnomalyIsRecorded(string anomaly)
    {
        var flag = Enum.Parse<SequenceFlags>(anomaly);
        Assert.Contains(recorder.Records, record => (record.Flags & flag) != 0);
    }

    [Then(@"the reported missing count is (\d+)")]
    public void ThenTheReportedMissingCountIs(long missingCount)
    {
        var missing = recorder.Records.Last(record => (record.Flags & SequenceFlags.Missing) != 0);
        Assert.Equal(missingCount, missing.MissingCount);
    }

    private sealed class RecordingSequenceLogger : ISequenceLogger
    {
        public readonly List<(SequenceFlags Flags, long MissingCount)> Records = new();

        public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose) { }

        public void UnregisterSubscription(Guid subscriptionId) { }

        public void Record(Guid subscriptionId, SequenceFlags flags, long missingCount) => Records.Add((flags, missingCount));

        public void Flush() { }
    }
}
