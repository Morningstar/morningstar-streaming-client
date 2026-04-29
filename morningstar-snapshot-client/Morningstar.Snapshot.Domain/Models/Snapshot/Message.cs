namespace Morningstar.Snapshot.Domain;

public class Message
{
    public required List<InvestmentMessage> Investments { get; set; }
    public required string Type { get; set; }
}
