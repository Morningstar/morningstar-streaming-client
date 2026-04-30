

using System.ComponentModel;
using System.Net;
using Morningstar.Snapshot.Domain.Models;

namespace Morningstar.Snapshot.Domain;

public class SnapshotResponse
{
    [property: DefaultValue(200)]
    public HttpStatusCode StatusCode { get; set; }
    public DataContainer Data { get; set; } = null!;
    public MetaData MetaData { get; set; } = null!;

    public string? Schema { get; set; }
}

public class DataContainer
{
    public List<InstrumentData> Realtime { get; set; } = new();
    public List<InstrumentData> Delayed { get; set; } = new();
}

public class InstrumentData
{
    [property: DefaultValue("0P000003PE")]
    public string PerformanceId { get; set; } = null!;
    public SortedDictionary<string, EventData> Events { get; set; } = new(StringComparer.Ordinal);
}

public class EventData
{
    [property: DefaultValue(12345)]
    public long? SequenceNumber { get; set; }
    [property: DefaultValue("2026-01-01T00:00:00.000Z")]
    public string? PublishTime { get; set; }
    public IMessage? Message { get; set; }
}

public class MetaData
{
    [property: DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string RequestId { get; set; } = null!;

    [property: DefaultValue("2026-01-01T00:00:00.000Z")]
    public string Time { get; set; } = null!;
    public List<WarningMessage> Messages { get; set; } = new();
}

public class WarningMessage
{
    [property: DefaultValue("0P000003PE")]
    public string PerformanceId { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
}
