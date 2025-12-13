using System.ClientModel;
using Asfoo.Models;
using Asfoor.Api.Agents;
using Asfoor.Api.Services;
using Asfoor.Api.Services.Executors;
using Asfoor.Api.Services.Ingestion;
using Asfoor.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Add services to the container.
builder.Services.AddHttpClient();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
var client = new OpenAIClient(new ApiKeyCredential(config["apiKey"] ?? throw new InvalidOperationException()),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(config["llmEndpoint"] ?? throw new InvalidOperationException()),
        NetworkTimeout = TimeSpan.FromMinutes(15)
    });
builder.Services.AddSingleton<OpenAIClient>(sp => client);
var embeddingGenerator = client.GetEmbeddingClient(config["embeddingModel"]).AsIEmbeddingGenerator();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

builder.Services.AddAGUI();


builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
builder.Services.AddSingleton<ChatHistoryIngestor>();
builder.Services.AddSingleton<DocumentIngestor>();
builder.Services.AddSingleton<Tools>();
builder.Services.AddSingleton<IAgentFactory, AgentFactory>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<ImageExecutor>();
builder.Services.AddSingleton<IntentExecutor>();
builder.Services.AddSingleton<SmartChatExecutor>();
builder.Services.AddKeyedSingleton("ingestion_directory",
    new DirectoryInfo(config["docPath"] ?? throw new InvalidOperationException()));


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/chat", async (ChatRequest request, IChatService chatService) =>
{
    var response = await chatService.ProcessChatRequestAsync(request);
    return Results.Ok(response);
});

app.MapPost("/smart-chat", async (ChatRequest request, IChatService chatService) =>
{
    var response = await chatService.ProcessChatRequestWithIntentAsync(request);
    return Results.Ok(response);
});

app.MapPost("/ingest", async (DocumentIngestor ingestor) =>
{
    await ingestor.IngestDocumentsAsync();
    return Results.Ok("Ingestion complete");
});

app.MapPost("/chat/history", async (ChatHistory request, ChatHistoryIngestor ingestor) =>
{
    // await ingestor.IngestThreadAsync(request.ConversationId, request.Messages);
    return Results.Ok();
});
var smartChatAgent = await app.Services.GetRequiredService<IAgentFactory>().CreateSmartChatAgentAsync();
var imageAgent = await app.Services.GetRequiredService<IAgentFactory>().CreateImageAgentAsync();

var smartAgent = new SmartChatAgent<ChatResponse>(imageAgent, smartChatAgent);


app.MapAGUI("agchat", smartAgent);
app.MapDefaultEndpoints();

await app.RunAsync();