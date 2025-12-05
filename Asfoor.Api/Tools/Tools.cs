using SemanticSearch = Asfoor.Api.Services.SemanticSearch;

namespace Asfoor.Api.Tools;

public class Tools(SemanticSearch semanticSearch)
{
    public async Task<IEnumerable<string>> SearchAsync(
        string searchPhrase,
      
        string? filenameFilter = null)
    {
        var results = await semanticSearch.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\">{result.Text}</result>");
    }
}