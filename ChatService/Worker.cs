using ChatService.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace ChatService;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        
        kernelBuilder.Services.AddHttpClient("WebApi", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7235");
        });

        #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        kernelBuilder.AddOllamaChatCompletion("qwen3:4b", new Uri("http://localhost:11434"));
        
        var kernel = kernelBuilder.Build();

        ChatCompletionAgent agent = new() // 👈🏼 Definition of the agent
        {
            Instructions = """
                           You are an assistant that manages a to-do list using only the todo_list_plugin.
                           Do not generate or assume any tasks on your own. Always retrieve tasks exclusively through the todo_list_plugin.
                           All task operations Create, Update, and Delete must be performed only via the todo_list_plugin.
                           Never invent task data or bypass the plugin.
                           The plugin is always right about the todo list information
                           After completing any operation, return the entire updated to-do list, retrieved exclusively via the todo_list_plugin
                           """,
            Name = "todo_list_assistant",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        agent.Kernel.ImportPluginFromType<WebApiPlugin>("todo_list_plugin");

        AgentThread thread = new ChatHistoryAgentThread();

        while (stoppingToken.IsCancellationRequested is false)
        {
            Console.Write("User: ");
            var message = Console.ReadLine();
            if (string.IsNullOrEmpty(message)) break; // Exit the loop if the message is empty

            Console.Write("\nAssistant: ");
            await foreach (var response in agent.InvokeStreamingAsync(message, thread, cancellationToken: stoppingToken))
            {
                Console.Write(response.Message.Content);
            }
            Console.WriteLine("\n");
        }
    }
}