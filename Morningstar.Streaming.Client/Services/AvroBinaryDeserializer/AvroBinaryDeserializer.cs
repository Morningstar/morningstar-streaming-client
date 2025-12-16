using Avro;
using Avro.Generic;
using Avro.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;

namespace Morningstar.Streaming.Client.Services.AvroBinaryDeserializer
{
    /// <summary>
    /// Service for deserializing Confluent Kafka Avro binary data into JSON string.
    /// Lazy loads the Avro schema on first deserialization.
    /// </summary>
    public class AvroBinaryDeserializer : IAvroBinaryDeserializer
    {
        private readonly IApiHelper apiHelper;
        private readonly AppConfig appConfig;
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<AvroBinaryDeserializer> logger;
        private readonly Lazy<Task<string?>> avroSchemaLazy;

        public AvroBinaryDeserializer(
            IApiHelper apiHelper,
            IOptions<AppConfig> appConfig,
            ILogger<AvroBinaryDeserializer> logger,
            ITokenProvider tokenProvider)
        {
            this.apiHelper = apiHelper;
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.appConfig = appConfig.Value;

            // Initialize lazy loading for Avro schema
            avroSchemaLazy = new Lazy<Task<string?>>(() =>
                LoadAvroSchemaAsync(), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public async Task<string> DeserializeAsync(byte[] binaryData)
        {
            if (binaryData.Length < 6)
                throw new InvalidOperationException("Invalid Avro payload.");

            // 1. Load schema (cached via Lazy)
            var schemaJson = await GetAvroSchemaAsync();
            if (string.IsNullOrWhiteSpace(schemaJson))
                throw new InvalidOperationException("Avro schema not loaded.");

            // 2. Strip Confluent header
            // byte 0   = magic byte (0)
            // byte 1-4 = schema id
            const int confluentHeaderSize = 5;

            if (binaryData[0] != 0x00)
                throw new InvalidOperationException("Unknown Avro magic byte.");

            var avroPayload = binaryData.AsSpan(confluentHeaderSize).ToArray();

            // 3. Parse schema
            var schema = Schema.Parse(schemaJson);

            // 4. Deserialize Avro binary
            using var inputStream = new MemoryStream(avroPayload);
            var decoder = new BinaryDecoder(inputStream);
            var reader = new GenericDatumReader<GenericRecord>(schema, schema);

            var record = reader.Read(null, decoder);

            // 5. Convert to JSON
            return ConvertGenericRecordToJson(record);
        }

        private static string ConvertGenericRecordToJson(GenericRecord record)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var field in record.Schema.Fields)
            {
                dict[field.Name] = ConvertAvroValue(record[field.Name]);
            }

            return System.Text.Json.JsonSerializer.Serialize(dict);
        }

        private static object? ConvertAvroValue(object? value)
        {
            if (value == null)
                return null;

            // Enum
            if (value is GenericEnum genericEnum)
                return genericEnum.Value;

            // Record
            if (value is GenericRecord record)
            {
                var dict = new Dictionary<string, object?>();

                foreach (var field in record.Schema.Fields)
                {
                    dict[field.Name] = ConvertAvroValue(record[field.Name]);
                }

                return dict;
            }

            // Array
            if (value is IList<object> list)
                return list.Select(ConvertAvroValue).ToList();

            // Primitive (string, long, int, double, bool, etc.)
            return value;
        }

        /// <summary>
        /// Gets the Avro schema, loading it lazily on first access.
        /// </summary>
        private Task<string?> GetAvroSchemaAsync() => avroSchemaLazy.Value;

        /// <summary>
        /// Loads the Avro schema from the API endpoint.
        /// </summary>
        private async Task<string?> LoadAvroSchemaAsync()
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                new KeyValuePair<string, string>("Accept", "application/json")
            };

            var schemaResponse = await apiHelper.ProcessRequestAsync<StreamResponse>(
                appConfig.StreamingApiBaseAddress + appConfig.AvroSchemaAddress,
                HttpMethod.Get,
                headers,
                null
            );

            if (schemaResponse != null && schemaResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return schemaResponse.Schema;
            }
            else
            {
                logger.LogError("Failed to load Avro schema. Status code: {StatusCode}", schemaResponse?.StatusCode);
                throw new InvalidOperationException("Failed to load Avro schema.");
            }
        }
    }
}
