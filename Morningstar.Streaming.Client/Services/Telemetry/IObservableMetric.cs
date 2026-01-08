namespace Morningstar.Streaming.Client.Services.Telemetry;

public interface IObservableMetric
{
    Task RecordMetric(string name, double value, IDictionary<string, string>? tags = null);
}

public interface IObservableMetric<T> where T : IMetric
{
    Task RecordMetric(string name, T value, IDictionary<string, string>? tags = null);
}
