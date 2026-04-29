using System.ComponentModel;

namespace Morningstar.Snapshot.Domain.Contracts;

public record InvestmentsRequest
{
    [property: DefaultValue("PerformanceId")]
    public string IdType { get; set; } = null!;
    [property: DefaultValue("[\"0P000003PE\"]")]
    public List<string> Ids { get; set; } = null!;
}
