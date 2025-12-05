using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace Asfoor.Api.Services;

public class IngestedChunk
{
    public const int
        VectorDimensions = 768; // // 384 dimensions for models like all-MiniLM-L6-v2, paraphrase-MiniLM-L6-v2

    public const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
    public const string CollectionName = "data-asfor-chunks";

    [VectorStoreData(StorageName = "key")]
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData(StorageName = "documentid")]
    [JsonPropertyName("documentid")]
    public required string DocumentId { get; set; }

    [VectorStoreData(StorageName = "content")]
    [JsonPropertyName("content")]
    public required string Text { get; set; }

    [VectorStoreData(StorageName = "context")]
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = "embedding")]
    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float>? Vector { get; set; }
}