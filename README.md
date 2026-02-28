# Blu J Zoom Phone Assistant

AI-powered Zoom Phone management assistant for Region One Education Service Center. Built with ASP.NET Core, Microsoft Semantic Kernel, and OpenAI GPT-4o.

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Zoom Server-to-Server OAuth App credentials
- OpenAI API key

### Configuration

Edit `appsettings.json` with your credentials:

```json
{
  "Zoom": {
    "AccountId": "your-account-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

### Run

```bash
dotnet run
```

The app starts at `https://localhost:5001`. Open it in a browser to use the chat interface.

### Deploy

```bash
dotnet publish -c Release -o ./publish
```

Upload the `publish/` folder to your hosting environment (Azure App Service, etc.).

## Project Structure

```
ZoomPhoneAgent/
├── Program.cs                  # App entry, DI setup, chat API endpoint
├── appsettings.json            # Configuration (credentials go here)
├── SystemPrompt.txt            # AI agent instructions
├── Services/
│   ├── ZoomAuthService.cs      # S2S OAuth token management
│   └── ZoomPhoneApiService.cs  # Zoom Phone API HTTP client
├── Plugins/
│   ├── UserPlugin.cs           # User lookups
│   ├── PhoneNumberPlugin.cs    # Phone number queries
│   ├── CallQueuePlugin.cs      # Call queue info
│   ├── AutoReceptionistPlugin.cs # IVR system queries
│   ├── ExtensionPlugin.cs      # Cross-type extension search
│   ├── CallLogPlugin.cs        # Call history reporting
│   ├── VoicemailPlugin.cs      # Voicemail status
│   └── DevicePlugin.cs         # Device management queries
├── wwwroot/
│   └── index.html              # Chat UI (embeddable via iframe)
└── docs/
    └── PROJECT_PROPOSAL.md     # Full project proposal
```

## Required Zoom API Scopes

- `phone:read:admin`
- `phone_call_log:read:admin`
- `phone_recording:read:admin`
- `phone_voicemail:read:admin`

## Embedding in Google Sites

Add an iframe to your Google Sites page:

```html
<iframe src="https://YOUR-APP-URL" width="100%" height="800" frameborder="0"></iframe>
```

## Documentation

See [docs/PROJECT_PROPOSAL.md](docs/PROJECT_PROPOSAL.md) for the full project proposal including scope, architecture, security considerations, and roadmap.
