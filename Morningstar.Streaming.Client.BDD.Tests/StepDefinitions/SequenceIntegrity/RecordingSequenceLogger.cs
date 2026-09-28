using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.BDD.Tests.StepDefinitions;

public class RecordingSequenceLogger : ISequenceLogger
{
    public readonly List<(SequenceFlags Flags, long MissingCount)> Records = new();

    public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose) { }

    public void UnregisterSubscription(Guid subscriptionId) { }

    public void Record(Guid subscriptionId, SequenceFlags flags, long missingCount) => Records.Add((flags, missingCount));

    public void Flush() { }
}