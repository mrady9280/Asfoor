using Microsoft.Extensions.VectorData;
using DataIngestor = Asfoor.Api.Services.Ingestion.DataIngestor;

namespace Asfoor.Api.Services;

public class SemanticSearch(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    [FromKeyedServices("ingestion_directory")]
    DirectoryInfo ingestionDirectory,
    DataIngestor dataIngestor)
{
    private Task? _ingestionTask;

    public async Task LoadDocumentsAsync() =>
        await (_ingestionTask ??= dataIngestor.IngestDataAsync(ingestionDirectory, searchPattern: "*.md"));
}