using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.AvroBinaryDeserializer;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain.Constants;
using Newtonsoft.Json;
using System.Net.WebSockets;

namespace Morningstar.Streaming.Client.Tests.ClientTests
{
    /// <summary>
    /// Integration coverage for the telemetry-loop routing seam
    /// (<see cref="StreamingApiClient.ProcessTelemetryItem"/>): drives real message JSON through
    /// deserialization → Admin/Snapshot exclusion → sequence classification → counter/latency.
    /// This closes the gap between the isolated <c>SequenceGapDetector</c> unit tests and the live
    /// pipeline (the wiring that previously let an Admin notice inflate the Unclassified metric).
    /// </summary>
    public class TelemetryRoutingIntegrationTests
    {
        private const string PerfId = "0P0000TEST";

        private readonly Guid subscriptionId = Guid.NewGuid();
        private readonly StreamingApiClient client;
        private readonly RecordingSequenceLogger sequence = new();
        private readonly RecordingCounterLogger counter = new();
        private readonly RecordingLatencyLogger latency = new();
        private readonly SequenceGapDetector detector;

        public TelemetryRoutingIntegrationTests()
        {
            client = new StreamingApiClient(
                Mock.Of<IApiHelper>(),
                Mock.Of<ILogger<StreamingApiClient>>(),
                Mock.Of<ITokenProvider>(),
                Mock.Of<IAvroBinaryDeserializer>(),
                null);

            detector = new SequenceGapDetector(subscriptionId, sequence);
        }

        [Fact]
        public void InOrderMarketData_IsClassified_AndCounted()
        {
            Feed(Message(EventTypes.Trade, PerfId, 1));

            sequence.LastFlags.Should().Be(SequenceFlags.InOrder);
            counter.Increments.Should().Be(1);
        }

        [Fact]
        public void ForwardGap_ReportsMissingWithGapSize()
        {
            Feed(Message(EventTypes.Trade, PerfId, 1));
            Feed(Message(EventTypes.Trade, PerfId, 5)); // 2, 3, 4 missing

            sequence.LastFlags.Should().Be(SequenceFlags.Missing);
            sequence.LastMissing.Should().Be(3);
            counter.Increments.Should().Be(2);
        }

        [Fact]
        public void AdminMessage_IsExcludedFromSequenceClassification_ButStillCounted()
        {
            Feed(Message(EventTypes.Admin, performanceId: null, sequenceNumber: null));

            sequence.Records.Should().BeEmpty();
            counter.Increments.Should().Be(1);
        }

        [Fact]
        public void SnapshotMessage_IsExcludedFromSequenceClassification_ButStillCounted()
        {
            Feed(Message(EventTypes.Snapshot, PerfId, sequenceNumber: null));

            sequence.Records.Should().BeEmpty();
            counter.Increments.Should().Be(1);
        }

        [Fact]
        public void MarketDataMissingSequenceNumber_IsUnclassified()
        {
            Feed(Message(EventTypes.Trade, PerfId, sequenceNumber: null));

            sequence.LastFlags.Should().Be(SequenceFlags.Unclassified);
        }

        [Fact]
        public void AvroArrayEnvelope_ResolvesEventType_AndIsClassified()
        {
            // Avro-derived JSON exposes the event type as a single-element "EventTypes" array;
            // the envelope maps it onto the singular EventType before classification.
            var json = JsonConvert.SerializeObject(new
            {
                EventTypes = new[] { EventTypes.Trade },
                PerformanceId = PerfId,
                SequenceNumber = 1L
            });

            Feed(json);

            sequence.Records.Should().ContainSingle();
            sequence.LastFlags.Should().Be(SequenceFlags.InOrder);
        }

        [Fact]
        public void MessageWithPublishTime_RecordsNonNegativeLatency()
        {
            // PublishTime is nanoseconds; the loop divides by 1e6 to get milliseconds.
            Feed(Message(EventTypes.Trade, PerfId, 1, publishTime: 900L * 1_000_000), receivedAtMillis: 1000);

            latency.Latencies.Should().ContainSingle().Which.Should().Be(100);
        }

        [Fact]
        public void UndeserializableMessage_IsSkipped_ButStillCounted()
        {
            Feed("null");

            sequence.Records.Should().BeEmpty();
            latency.Latencies.Should().BeEmpty();
            counter.Increments.Should().Be(1);
        }

        private void Feed(string json, long receivedAtMillis = 0)
            => client.ProcessTelemetryItem(
                subscriptionId,
                new StreamingApiClient.TelemetryItem(WebSocketMessageType.Text, json, receivedAtMillis),
                detector,
                counter,
                latency);

        private static string Message(string? eventType, string? performanceId, long? sequenceNumber, long? publishTime = null)
            => JsonConvert.SerializeObject(new
            {
                EventType = eventType,
                PerformanceId = performanceId,
                SequenceNumber = sequenceNumber,
                PublishTime = publishTime
            });

        private sealed class RecordingSequenceLogger : ISequenceLogger
        {
            public readonly List<(SequenceFlags Flags, long MissingCount)> Records = new();

            public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose) { }

            public void UnregisterSubscription(Guid subscriptionId) { }

            public void Record(Guid subscriptionId, SequenceFlags flags, long missingCount) => Records.Add((flags, missingCount));

            public void Flush() { }

            public SequenceFlags LastFlags => Records[^1].Flags;

            public long LastMissing => Records[^1].MissingCount;
        }

        private sealed class RecordingCounterLogger : ICounterLogger
        {
            public int Increments { get; private set; }

            public void RegisterSubscription(Guid subscriptionId, Guid userId, string serializationFormat, string? purpose) { }

            public void UnregisterSubscription(Guid subscriptionId) { }

            public void Increment(Guid subscriptionId) => Increments++;

            public void Flush() { }
        }

        private sealed class RecordingLatencyLogger : ILatencyLogger
        {
            public readonly List<long> Latencies = new();

            public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose) { }

            public void UnregisterSubscription(Guid subscriptionId) { }

            public void RecordLatency(Guid subscriptionId, long millis) => Latencies.Add(millis);

            public void Flush() { }
        }
    }
}
