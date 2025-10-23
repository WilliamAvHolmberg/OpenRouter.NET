# Basic CLI Sample

A beautiful interactive command-line application demonstrating the OpenRouter.NET SDK with **Spectre.Console**.

## Features

- 🎨 **Beautiful UI** - Styled with Spectre.Console
- 📊 **List Models** - Browse available models in a formatted table
- 💬 **Chat with Streaming** - Real-time streaming responses with TTFT metrics
- 🔁 **Conversation Mode** - Multi-turn conversations with history management
- 📄 **Artifact Support** - Automatically detects and displays code/document artifacts
- 🔍 **Smart Model Search** - Type to search through hundreds of models with autocomplete
- ⌨️ **Keyboard Navigation** - Arrow keys + Enter for easy selection

## Setup

1. Get your API key from [OpenRouter](https://openrouter.ai/)

2. Set your API key as an environment variable:

   **macOS/Linux:**
   ```bash
   export OPENROUTER_API_KEY="your-key-here"
   ```

   **Windows (PowerShell):**
   ```powershell
   $env:OPENROUTER_API_KEY="your-key-here"
   ```

3. Run the sample:
   ```bash
   cd samples/BasicCliSample
   dotnet run
   ```

## Usage

### Main Menu
Navigate with arrow keys and press Enter to select:
- **List available models** - Shows models in a beautiful table with pricing info
- **Send chat message (streaming)** - One-off chat with real-time streaming and artifact support
- **Start conversation** - Multi-turn conversation with context memory
- **Exit** - Close the application

### Conversation Mode
When in conversation mode, you can use special commands:
- **/exit** - Leave conversation mode
- **/clear** - Clear conversation history
- **/history** - View full conversation with token estimates

### Model Selection
When selecting a model, you can:
- **Type to search** - Start typing to filter models (e.g., "gpt", "claude", "llama")
- **Arrow keys** - Navigate through the filtered list
- **Enter** - Select the highlighted model

Popular models are shown first for convenience!

## Dependencies

- **Spectre.Console** - For beautiful interactive CLI experience
