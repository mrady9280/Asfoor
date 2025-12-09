using System.ClientModel;
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
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection)
{
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