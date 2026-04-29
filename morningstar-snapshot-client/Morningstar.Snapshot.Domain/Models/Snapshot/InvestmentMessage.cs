namespace Morningstar.Snapshot.Domain;

public class InvestmentMessage
{
    public required string Id { get; set; }
    public required string IdType { get; set; }
    public required string Status { get; set; }
    public required string ErrorCode { get; set; }
}
