# WhisperPete Developer Notes

Version 1.0.1.

WhisperPete 1.0.1 uses Rust, Tauri 2, `cpal` capture, sherpa-onnx Rust bindings, and Parakeet TDT local transcription.

## Build

From this directory:

```powershell
cargo check
cargo build
cargo tauri build --bundles nsis
```

The first build downloads the pinned Rust dependencies and sherpa-onnx native runtime archive. Tauri also requires the Windows WebView2 runtime.

## Run

Run the app with `cargo run`. On first launch, click **Install speech model** to download and install the Parakeet TDT INT8 model. Set `WHISPERPETE_MODEL_DIR` to use another model directory.

The app opens a small window and stays in the tray. Click Start, speak, and click Stop; the successful transcript is copied to the Windows clipboard so you can paste it with `Ctrl+V`. The `Alt+Shift+Space` hotkey starts/stops recording. The Stop button remains responsive while transcription runs.

The application is Windows-only, uses the CPU inference provider, and intentionally copies transcripts to the Windows clipboard rather than injecting text into the active window.

The NSIS installer is generated at `target\release\bundle\nsis\WhisperPete_1.0.1_x64-setup.exe`.
