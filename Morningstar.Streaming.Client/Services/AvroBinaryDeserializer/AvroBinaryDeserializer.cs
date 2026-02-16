using System.Buffers;
using System.Runtime.InteropServices;
using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Newtonsoft.Json;

namespace Morningstar.Streaming.Client.Services.AvroBinaryDeserializer
{
    /// <summary>
    /// Service for deserializing Confluent Kafka Avro binary data into JSON string or strongly-typed objects.
    /// Supports both GenericRecord and SpecificRecord deserialization.
    /// Lazy loads the Avro schema on first deserialization.
    /// </summary>
    public class AvroBinaryDeserializer : IAvroBinaryDeserializer
    {

         private static class ReaderCache<TSpecificRecord>
        where TSpecificRecord : class, ISpecificRecord, new()
        {
            public static Avro.Schema Schema { get; } = new TSpecificRecord().Schema;

            public static ThreadLocal<SpecificDatumReader<TSpecificRecord>> Reader { get; } =
                new(() => new SpecificDatumReader<TSpecificRecord>(Schema, Schema));
        }

        private readonly IApiHelper apiHelper;
        private readonly AppConfig appConfig;
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<AvroBinaryDeserializer> logger;
        private readonly Lazy<Task<string?>> avroSchemaLazy;
        private readonly Lazy<Task<Schema>> avroParsedSchemaLazy;
        private readonly object genericReaderInitGate = new();
        private volatile ThreadLocal<GenericDatumReader<GenericRecord>>? genericRecordReader;
        private Task? genericReaderInitTask;

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

            // Parse schema once (uses cached schema JSON from avroSchemaLazy)
            avroParsedSchemaLazy = new Lazy<Task<Schema>>(
                LoadParsedSchemaAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public async Task<T?> DeserializeAsync<T>(byte[] binaryData)
        {
            // byte 0   = magic byte (0)
            // byte 1-4 = schema id
            const int confluentHeaderSize = 5;

            if (binaryData.Length <= confluentHeaderSize)
            {
                throw new InvalidOperationException("Invalid Avro payload.");
            }

            if (binaryData[0] != 0x00)
            {
                throw new InvalidOperationException("Unknown Avro magic byte.");
            }

            // Ensure schema and reader are loaded/cached
            var readerThreadLocal = genericRecordReader ?? await EnsureGenericRecordReaderAsync();

            // Deserialize Avro binary directly from the original buffer (avoid payload copy)
            var payloadLength = binaryData.Length - confluentHeaderSize;
            using var inputStream = new MemoryStream(binaryData, confluentHeaderSize, payloadLength, writable: false);
            var decoder = new BinaryDecoder(inputStream);
            var reader = readerThreadLocal.Value!;
            var record = reader.Read(null!, decoder);

            return ConvertGenericRecordToJson<T>(record);
        }

        public TSpecificRecord DeserializeSpecific<TSpecificRecord>(ReadOnlyMemory<byte> binaryData)
           where TSpecificRecord : class, ISpecificRecord, new()
        {
            if (binaryData.Length < 6)
            {
                throw new InvalidOperationException("Invalid Avro payload.");
            }

            if (binaryData.Span[0] != 0x00)
            {
                throw new InvalidOperationException("Unknown Avro magic byte.");
            }

            // Strip Confluent header
            // byte 0   = magic byte (0)
            // byte 1-4 = schema id
            const int confluentHeaderSize = 5;

            // Fast-path: avoid copying the payload as the ReadOnlyMemory is backed by a byte[].
            if (MemoryMarshal.TryGetArray(binaryData, out var segment) && segment.Array != null)
            {
                var payloadOffset = segment.Offset + confluentHeaderSize;
                var payloadLength = segment.Count - confluentHeaderSize;
                if (payloadLength <= 0)
                {
                    throw new InvalidOperationException("Invalid Avro payload.");
                }

                using var inputStream = new MemoryStream(segment.Array, payloadOffset, payloadLength, writable: false);
                var decoder = new BinaryDecoder(inputStream);
                var reader = ReaderCache<TSpecificRecord>.Reader.Value!;
                return reader.Read(default!, decoder);
            }

            // Fallback: rent buffer from pool and copy payload.
            var fallbackPayloadLength = binaryData.Length - confluentHeaderSize;
            var buffer = ArrayPool<byte>.Shared.Rent(fallbackPayloadLength);

            try
            {
                // Copy payload portion to rented buffer
                binaryData.Slice(confluentHeaderSize).Span.CopyTo(buffer);

                using var inputStream = new MemoryStream(buffer, 0, fallbackPayloadLength, writable: false);
                var decoder = new BinaryDecoder(inputStream);
                var reader = ReaderCache<TSpecificRecord>.Reader.Value!;

                var record = reader.Read(default!, decoder);
                return record;
            }
            finally
            {
                // Return buffer to pool
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static T? ConvertGenericRecordToJson<T>(GenericRecord record)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var field in record.Schema.Fields)
            {
                dict[field.Name] = ConvertAvroValue(record[field.Name]);
            }

            var serializedDict = System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            if (typeof(T) == typeof(string))
            {
                return (T)(object)serializedDict;
            }

            return JsonConvert.DeserializeObject<T>(serializedDict);
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

        private Task<Schema> GetParsedAvroSchemaAsync() => avroParsedSchemaLazy.Value;

        private async Task<Schema> LoadParsedSchemaAsync()
        {
            var schemaJson = await GetAvroSchemaAsync();
            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                throw new InvalidOperationException("Avro schema not loaded.");
            }

            return Schema.Parse(schemaJson);
        }

        private async Task<ThreadLocal<GenericDatumReader<GenericRecord>>> EnsureGenericRecordReaderAsync()
        {
            var cached = genericRecordReader;
            if (cached != null)
            {
                return cached;
            }

            Task initTask;
            lock (genericReaderInitGate)
            {
                cached = genericRecordReader;
                if (cached != null)
                {
                    return cached;
                }

                genericReaderInitTask ??= InitGenericRecordReaderAsync();
                initTask = genericReaderInitTask;
            }

            await initTask;
            return genericRecordReader!;
        }

        private async Task InitGenericRecordReaderAsync()
        {
            var schema = await GetParsedAvroSchemaAsync();
            genericRecordReader = new ThreadLocal<GenericDatumReader<GenericRecord>>(
                () => new GenericDatumReader<GenericRecord>(schema, schema));
        }

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
