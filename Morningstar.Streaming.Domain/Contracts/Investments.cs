namespace Morningstar.Streaming.Domain.Contracts
{
    public class Investments
    {
        public string IdType { get; set; } = null!;
        public List<string> Ids { get; set; } = null!;
    }
}
