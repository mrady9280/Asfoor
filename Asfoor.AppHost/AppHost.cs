using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var llmEndpoint = builder.AddParameter("llmEndpoint", secret: true);
var embeddingModel = builder.AddParameter("embeddingModel", secret: true);
var chatModel = builder.AddParameter("chatModel", secret: true);
var imageModel = builder.AddParameter("imageModel", secret: true);
var apiKey = builder.AddParameter("apiKey", secret: true);
var documentPath = builder.AddParameter("docPath", secret: true);
var webSearchKey = builder.AddParameter("webSearchKey", secret: true);
var webSearchApi = builder.AddParameter("webSearchApi", secret: true);
var vectorDbPath = builder.AddParameter("vectorDbPath", secret: true);

var vectorDB = builder.AddQdrant("vectordb")
    .WithDataBindMount("/Users/mrady/Library/Mobile Documents/com~apple~CloudDocs/vectordb-data/");

var markitdown = builder.AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http");

var api = builder.AddProject<Asfoor_Api>("api");
api.WithEnvironment("llmEndpoint", llmEndpoint)
    .WithEnvironment("embeddingModel", embeddingModel)
    .WithEnvironment("chatModel", chatModel)
    .WithEnvironment("imageModel", imageModel)
    .WithEnvironment("ApiKey", apiKey)
    .WithEnvironment("docPath", documentPath)
    .WithEnvironment("webSearchKey", webSearchKey)
    .WithEnvironment("webSearchApi", webSearchApi);
api
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
api
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

var web = builder.AddProject<Asfoor_Web>("web")
    .WithReference(api).WaitFor(api);

// var app = builder.AddProject<Asfoor>("app")
//     .WithReference(api).WaitFor(api);

builder.Build().Run();