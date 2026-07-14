using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LocalShoppingAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add CORS so frontends can call this API
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 2. Fetch the Gemini API Key from your Environment Variables
            string geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                ?? throw new InvalidOperationException("CRITICAL: 'GEMINI_API_KEY' environment variable is missing!");

            // 3. Register the Semantic Kernel in the Dependency Injection (DI) Container
            builder.Services.AddTransient<Kernel>(sp =>
            {
                var kernelBuilder = Kernel.CreateBuilder();

                // Swap Ollama with the Gemini API (using the fast and free gemini-1.5-flash model)
                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: "gemini-1.5-flash",
                    apiKey: geminiApiKey
                );

                // Register your ProductPlugin class as a tool set
                kernelBuilder.Plugins.AddFromType<ProductPlugin>("ShoppingTools");

                return kernelBuilder.Build();
            });

            var app = builder.Build();

            // Enable CORS & routing
            app.UseCors();

            // 4. Create a stateless POST endpoint to receive chat messages
            app.MapPost("/api/chat", async (ChatRequest request, Kernel kernel) =>
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return Results.BadRequest(new { error = "Message cannot be empty." });
                }

                try
                {
                    var chatService = kernel.GetRequiredService<IChatCompletionService>();

                    // Prepare the history object
                    var chatHistory = new ChatHistory();
                    
                    // Inject system prompt behavior
                    chatHistory.AddSystemMessage(
                        "You are a helpful cloud-hosted AI Shopping Assistant. Use your provided tools to look up product data " +
                        "and calculate promotional code updates. Always report details clearly in structured markdown layout.");

                    // (Optional) Rebuild history from client if sent, or just process the current user message
                    if (request.History != null)
                    {
                        foreach (var hist in request.History)
                        {
                            if (hist.IsUser) chatHistory.AddUserMessage(hist.Text);
                            else chatHistory.AddAssistantMessage(hist.Text);
                        }
                    }

                    // Add the latest user message
                    chatHistory.AddUserMessage(request.Message);

                    // Configure tool call settings for Gemini
                    GeminiPromptExecutionSettings settings = new()
                    {
                        ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
                    };

                    // Let Gemini run, automatically call any needed ProductPlugin tools, and return the answer
                    var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);

                    return Results.Ok(new ChatResponse 
                    { 
                        Reply = response.Content ?? "I couldn't process that request." 
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { error = $"Execution error: {ex.Message}" }, statusCode: 500);
                }
            });

            app.MapGet("/", () => "Healthy!");
            app.Run();
        }
    }

    // --- Supporting Data Transfer Objects (DTOs) ---

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public HistoryItem[]? History { get; set; }
    }

    public class HistoryItem
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
    }
}