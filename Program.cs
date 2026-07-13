using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LocalShoppingAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
         // 1. Create a custom HttpClient with an extended or infinite timeout
var handler = new HttpClientHandler();
var httpClient = new HttpClient(handler)
{
    Timeout = System.Threading.Timeout.InfiniteTimeSpan // Prevents the 100-second cutoff
};

// 2. Pass the httpClient into your Ollama configuration setup
var builder = Kernel.CreateBuilder();

builder.AddOpenAIChatCompletion(
    modelId: "llama3.1",                            // Or "qwen2.5:3b"
    apiKey: "ollama-offline",
    endpoint: new Uri("http://localhost:11434/v1"),
    httpClient: httpClient                          // <-- Hook up the custom client here
);

            // 2. Register our custom retail tool plugin
            builder.Plugins.AddFromType<ProductPlugin>("ShoppingTools");

            Kernel kernel = builder.Build();

            // 3. Enable automatic tool call invocation behavior for step-by-step loops
            OpenAIPromptExecutionSettings settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var chatHistory = new ChatHistory();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Give the local model its operational behavior guidelines
            chatHistory.AddSystemMessage(
                "You are an offline AI Shopping Assistant running locally. Use your provided tools to look up product data " +
                "and calculate promotional code updates. Always report details clearly in structured markdown layout.");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🛍️ Offline Ollama Shopping Agent Activated! (Ctrl+C to exit)");
            Console.WriteLine("Ask something like: 'Do you have any mechanical keyboards?' or 'Is the gaming mouse in stock?'");
            Console.ResetColor();

            // 4. Main loop processing conversation inputs
            while (true)
            {
                Console.Write("\n👤 You: ");
                string? userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput)) break;

                chatHistory.AddUserMessage(userInput);

                try
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("⏳ Agent is parsing request and executing local tools...");
                    Console.ResetColor();

                    // Model evaluates text, calls local functions if needed, and returns a processed outcome
                    var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n🤖 Agent:\n{response.Content}");
                    Console.ResetColor();

                    chatHistory.Add(response);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"⚠️ Execution error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }
}