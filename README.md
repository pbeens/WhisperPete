# WhisperPete (v0.5.0 Beta)

**WhisperPete** is a local-first, high-performance speech-to-text (STT) application for Windows. It provides a seamless, system-wide dictation experience using OpenAI's Whisper models, optimized with Microsoft Olive and running locally via ONNX Runtime and DirectML.

## 🚀 Features

- **Local-First Privacy**: No audio ever leaves your machine. Everything happens on your local hardware.
- **Hardware Acceleration Detection**: Real-time display of whether you are using your GPU (DirectML) or CPU.
- **Marathon Mode**: Automated 30-second audio chunking for unlimited dictation duration.
- **Global Hotkey & Overlay**: Press `Ctrl + Alt + W` to start/stop dictation with a heads-up flashing overlay showing live instructions.
- **Persistent Storage**: All settings and logs are saved to `%LOCALAPPDATA%\WhisperPete` for stability across builds.
- **Seamless Injection**: Transcribed text is automatically injected into your active window.
- **Lightweight Tray App**: Runs in the background with a minimal footprint.

## 🛠️ Setup & Build Instructions

### Prerequisites

- **Visual Studio 2022** (version 17.8 or later recommended).
- **.NET 8.0 SDK** installed.
- **Git** for version control.

### Installation

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/Username/WhisperPete.git
   cd WhisperPete
   ```

2. **Restore Dependencies**:

   ```bash
   dotnet restore
   ```

3. **Build the Solution**:
   - **CLI**: `dotnet build --configuration Release`
   - **Visual Studio**: Open `WhisperPete.sln` and press **Ctrl+Shift+B**.

## 🤖 Agentic Workflows (Skills)

WhisperPete includes built-in **Agentic Workflows** (Skills) located in `.agents/workflows/`. These are designed for use with AI coding assistants (like Antigravity) to automate repetitive tasks.

Project-wide agent guidance is in [`AGENTS.md`](AGENTS.md), active handoff tasks are in [`tasks.md`](tasks.md), and program-related prompt history is maintained in [`prompts.md`](prompts.md).

| Command | Purpose |
| :--- | :--- |
| `@[/rebuild]` | Automatically stops any running instances, cleans the solution, and performs a fresh, non-incremental build of both Release and Debug configurations. |
| `@[/run]` | Launches the optimized **Release** build of WhisperPete.Tray immediately. |

### How to use

In an agentic environment (like the Antigravity chat), simply mention the skill (e.g., "Please `@[/rebuild]` the app") to trigger the automated sequence of commands. This ensures consistency and reduces manual errors for both human developers and AI collaborators.

### How to Run

Once built, the application is ready to launch:

- **Executable**: `WhisperPete.Tray\bin\Release\net8.0-windows\WhisperPete.Tray.exe`
- **Background**: Look for the **Cyan Soundwave Icon** in your system tray!

### Model Setup

WhisperPete works best with Optimized Whisper ONNX models:

1. Download a model (e.g., [whisper-olive](https://huggingface.co/thewh1teagle/whisper-olive)).
2. Right-click the tray icon -> **Settings**.
3. Select your `.onnx` file and watch the "Compute Device" indicator light up!

## ⚙️ Requirements

- **OS**: Windows 10/11 (x64)
- **Runtime**: .NET 8.0 Runtime
- **GPU**: DirectX 12 compatible GPU (NVIDIA RTX 30-series or equivalent recommended)

## 📜 License

This project is licensed under the **MIT License**. See the `LICENSE` file for details.

## 🤝 Credits

Developed as a high-performance alternative to existing transcription tools, focusing on speed, stability, and privacy.
