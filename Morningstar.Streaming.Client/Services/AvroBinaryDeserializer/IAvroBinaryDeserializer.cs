using Avro.Specific;

namespace Morningstar.Streaming.Client.Services.AvroBinaryDeserializer
{
    /// <summary>
    /// Service for deserializing Avro binary data into JSON strings or strongly-typed objects.
    /// </summary>
    public interface IAvroBinaryDeserializer
    {
        /// <summary>
        /// Deserializes Avro binary data using the loaded schema and GenericRecord.
        /// </summary>
        /// <param name="binaryData">The binary data to deserialize</param>
        /// <returns>Typed representation of the deserialized data</returns>
        Task<T?> DeserializeAsync<T>(byte[] binaryData);

        /// <summary>
        /// Deserializes Avro binary data using SpecificRecord for strongly-typed Avro objects.
        /// </summary>
        /// <typeparam name="TSpecificRecord">The Avro SpecificRecord type that implements ISpecificRecord</typeparam>
        /// <param name="binaryData">The binary data to deserialize</param>
        /// <returns>The strongly-typed SpecificRecord instance</returns>
        Task<TSpecificRecord?> DeserializeSpecificAsync<TSpecificRecord>(byte[] binaryData)
            where TSpecificRecord : class, ISpecificRecord, new();
    }
}
