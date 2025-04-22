using ChatService;
using Microsoft.SemanticKernel;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient("WebApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7235");
});

var kernelBuilder = builder.Services.AddKernel();

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
kernelBuilder.AddOllamaChatCompletion("llama3.2", new Uri("http://localhost:11434"));

var host = builder.Build();
host.Run();

