using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using ZoomPhoneAgent.Plugins;
using ZoomPhoneAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Register Zoom services ---
builder.Services.AddSingleton<ZoomAuthService>();
builder.Services.AddSingleton<ZoomPhoneApiService>();
builder.Services.AddHttpClient<ZoomAuthService>();
builder.Services.AddHttpClient<ZoomPhoneApiService>();

// --- Register Semantic Kernel ---
var openAiKey = builder.Configuration["OpenAI:ApiKey"]!;
var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o";

builder.Services.AddSingleton<Kernel>(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiKey);

    var kernel = kernelBuilder.Build();

    // Register all Zoom Phone plugins
    var api = sp.GetRequiredService<ZoomPhoneApiService>();
    kernel.Plugins.AddFromObject(new UserPlugin(api), "Users");
    kernel.Plugins.AddFromObject(new PhoneNumberPlugin(api), "PhoneNumbers");
    kernel.Plugins.AddFromObject(new CallQueuePlugin(api), "CallQueues");
    kernel.Plugins.AddFromObject(new AutoReceptionistPlugin(api), "AutoReceptionists");
    kernel.Plugins.AddFromObject(new ExtensionPlugin(api), "Extensions");
    kernel.Plugins.AddFromObject(new CallLogPlugin(api), "CallLogs");
    kernel.Plugins.AddFromObject(new VoicemailPlugin(api), "Voicemails");
    kernel.Plugins.AddFromObject(new DevicePlugin(api), "Devices");

    return kernel;
});

// --- Configure CORS for iframe embedding ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://sites.google.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

// --- Load system prompt ---
var systemPrompt = await File.ReadAllTextAsync(
    Path.Combine(app.Environment.ContentRootPath, "SystemPrompt.txt"));

// --- Chat API endpoint ---
app.MapPost("/api/chat", async (ChatRequest request, Kernel kernel) =>
{
    var chatService = kernel.GetRequiredService<IChatCompletionService>();

    var history = new ChatHistory();
    history.AddSystemMessage(systemPrompt);

    // Replay conversation history from the client
    foreach (var msg in request.Messages)
    {
        if (msg.Role == "user")
            history.AddUserMessage(msg.Content);
        else if (msg.Role == "assistant")
            history.AddAssistantMessage(msg.Content);
    }

    // Add the new user message
    history.AddUserMessage(request.UserMessage);

    // Execute with automatic function calling enabled
    var settings = new OpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    var response = await chatService.GetChatMessageContentAsync(history, settings, kernel);

    return Results.Json(new { reply = response.Content });
});

// --- Health check endpoint ---
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", agent = "Blu J Zoom Phone Assistant" }));

// --- Serve the chat UI ---
app.MapFallbackToFile("index.html");

app.Run();

// --- Request/Response models ---
public record ChatRequest(string UserMessage, List<ChatMessage> Messages);
public record ChatMessage(string Role, string Content);
