namespace Morningstar.Streaming.Client.Services.AvroBinaryDeserializer
{
    /// <summary>
    /// Service for deserializing Avro binary data into JSON strings or strongly-typed objects.
    /// </summary>
    public interface IAvroBinaryDeserializer
    {
        /// <summary>
        /// Deserializes Avro binary data using the loaded schema.
        /// </summary>
        /// <param name="binaryData">The binary data to deserialize</param>
        /// <returns>JSON string representation of the deserialized data</returns>
        Task<string> DeserializeAsync(byte[] binaryData);
    }
}
