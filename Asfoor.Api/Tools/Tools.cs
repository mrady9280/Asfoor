using System.ClientModel;
using System.Text.Json;
using System.Web;
using Asfoo.Models;
using Asfoor.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

namespace Asfoor.Api.Tools;

public class Tools(
    OpenAIClient openAiClient,
    IConfiguration configuration,
    ILogger<Tools> logger,
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    IHttpClientFactory httpClientFactory)
{
    public async Task<string> WebSearchAsync(string query)
    {
        var apiKey = configuration["webSearchKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Brave Search API key is missing. Skipping web search.");
            return "Web search is currently unavailable because the Brave Search API key is not configured.";
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            var encodedQuery = HttpUtility.UrlEncode(query);
            var uri = $"{configuration["webSearchApi"]}/web/search?q={encodedQuery}&count=5";

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("X-Subscription-Token", apiKey);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // Brave Search response structure: { "web": { "results": [ ... ] } }
            if (doc.RootElement.TryGetProperty("web", out var web) && 
                web.TryGetProperty("results", out var results))
            {
                var searchResults = results.EnumerateArray()
                    .Select(x => new
                    {
                        Title = x.GetProperty("title").GetString(),
                        Url = x.GetProperty("url").GetString(),
                        Snippet = x.TryGetProperty("description", out var desc) ? desc.GetString() : "No description available"
                    })
                    .Aggregate("", (current, item) => current + $"Title: {item.Title}\nURL: {item.Url}\nSnippet: {item.Snippet}\n\n");

                return string.IsNullOrWhiteSpace(searchResults) ? "No web search results found." : searchResults;
            }

            return "No web pages found in search results.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing web search for query: {Query}", query);
            return $"An error occurred while performing web search: {ex.Message}";
        }
    }

    public async Task<IEnumerable<string>> SearchAsync(
        string searchPhrase,
        string? filenameFilter = null)
    {
        var results = await SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\">{result.Text}</result>");
    }

    public Func<string, Task<string>> CreateAnalyzeImagesFunc(List<ChatFileAttachment> attachments)
    {
        var imageModel = configuration[ChatServiceConstants.ImageModelConfigKey]
                         ?? throw new InvalidOperationException(
                             $"Configuration key '{ChatServiceConstants.ImageModelConfigKey}' is missing.");

        return async (string query) =>
        {
            if (attachments == null || !attachments.Any())
            {
                return "No images were attached to this request.";
            }

            logger.LogInformation("Agent requesting image analysis for query: {Query}", query);

                var imageAgent = openAiClient.GetChatClient(imageModel)
                .AsIChatClient()
                .CreateAIAgent(new ChatClientAgentOptions
                {
                    Instructions =
                        "You are a helpful assistant that analyzes images. Describe the images or answer questions about them."
                });

            var contents = new List<AIContent> { new TextContent(query) };

            foreach (var attachment in attachments)
            {
                contents.Add(new DataContent(attachment.Data, attachment.ContentType));
            }

            var userMessage = new ChatMessage(ChatRole.User, contents);

            var response = await imageAgent.RunAsync(userMessage);
            return response.ToString();
        };
    }

    private async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });

        return await nearest.Select(result => result.Record).ToListAsync();
    }
}