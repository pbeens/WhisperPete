# Tasks

## WhisperPete 1.0.1

### Completed

- [x] Build and stage the optimized Windows release executable.
- [x] Add the version number to the popup window.
- [x] Add the Buy Me a Coffee link to the popup and README.
- [x] Provide simple executable and Parakeet model setup instructions.
- [x] Verify that the release executable starts successfully.

### Remaining validation

- [ ] Test a complete recording and transcription with the Parakeet model installed.
- [ ] Verify the global `Alt+Shift+Space` hotkey starts and stops recording while another application is focused.
- [ ] Verify the completed transcript is copied correctly to the Windows clipboard.
- [ ] Test microphone errors, missing-model errors, high-DPI display, tray behavior, and window resizing.
- [ ] Test the release executable on a clean Windows machine with the documented setup steps.

### Known 1.0.1 behavior

- Windows is the supported platform.
- Transcription runs locally using Parakeet through sherpa-onnx.
- The model is downloaded separately and stored under `%LOCALAPPDATA%\WhisperPete\models\`.
- The application uses the CPU inference provider.
- Successful transcripts are copied to the clipboard for manual pasting with `Ctrl+V`.
- Automatic text injection is not included in this release.

## Handoff

The active product is the Rust/Tauri application in `whisperpete-rs/`. The release artifact is `release/WhisperPete.exe`.
