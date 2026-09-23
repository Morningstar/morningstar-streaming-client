using Morningstar.Streaming.Client.Services.Telemetry;
using TechTalk.SpecFlow;

namespace Morningstar.Streaming.Client.BDD.Tests.StepDefinitions;

[Binding]
public partial class SequenceIntegrityStepDefinitions
{
    private const string DefaultPerformanceId = "0P0000TEST";
    private const string DefaultEventType = "Trade";

    private readonly Guid subscriptionId = Guid.NewGuid();
    private readonly RecordingSequenceLogger sequenceRecorder = new();
    private SequenceGapDetector sequenceGapDetector = null!;

    [Given(@"a sequence detector")]
    public void GivenASequenceDetector()
    {
        sequenceGapDetector = new SequenceGapDetector(subscriptionId, sequenceRecorder);
    }

    [Given(@"a sequence detector with a tracking window of (\d+) sequences")]
    public void GivenASequenceDetectorWithWindow(int window)
    {
        sequenceGapDetector = new SequenceGapDetector(subscriptionId, sequenceRecorder, window);
    }


    [When(@"messages arrive with sequence numbers (.*)")]
    public void WhenMessagesArriveWithSequenceNumbers(string sequenceNumbers)
    {
        foreach (var value in sequenceNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            sequenceGapDetector.Process(DefaultPerformanceId, DefaultEventType, long.Parse(value));
        }
    }

    [When(@"a message arrives with no sequence number")]
    public void WhenAMessageArrivesWithNoSequenceNumber()
    {
        sequenceGapDetector.Process(DefaultPerformanceId, DefaultEventType, null);
    }

    [When(@"a message arrives for instrument ""(.*)"" event ""(.*)"" with sequence (\d+)")]
    public void WhenAMessageArrivesForInstrument(string performanceId, string eventType, long sequenceNumber)
    {
        sequenceGapDetector.Process(performanceId, eventType, sequenceNumber);
    }

    [Then(@"no sequence anomaly is recorded")]
    public void ThenNoSequenceAnomalyIsRecorded()
    {
        Assert.All(sequenceRecorder.Records, record => Assert.Equal(SequenceFlags.InOrder, record.Flags));
    }

    [Then(@"an? ""(.*)"" anomaly is recorded")]
    public void ThenAnomalyIsRecorded(string anomaly)
    {
        var flag = Enum.Parse<SequenceFlags>(anomaly);
        Assert.Contains(sequenceRecorder.Records, record => (record.Flags & flag) != 0);
    }

    [Then(@"the reported missing count is (\d+)")]
    public void ThenTheReportedMissingCountIs(long missingCount)
    {
        var missing = sequenceRecorder.Records.Last(record => (record.Flags & SequenceFlags.Missing) != 0);
        Assert.Equal(missingCount, missing.MissingCount);
    }
}
