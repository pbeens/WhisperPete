# Tasks

## Current focus

- [x] Retrofit the repository with the vendor-neutral agentic project structure.
- [x] Establish `AGENTS.md` as the durable project instruction file.
- [x] Normalize the prompt log and remove the legacy Gemini instruction-file reference.
- [x] Perform a first post-initialization repository and technology audit.
- [x] Write transcription-library recommendations in `recommendations.md`.

## Next tasks

- [ ] Review [`recommendations.md`](recommendations.md) and decide whether the first engine prototype is Whisper.net or sherpa-onnx + Parakeet. Do not change the runtime until that choice is made.
- [ ] Establish a reproducible development baseline: install the .NET 8 SDK, add an intentional `global.json` policy, confirm restore/build, and document the exact Windows/SDK/runtime prerequisites.
- [ ] Create a small automated test project for audio conversion, settings migration, model-contract validation, text cleanup, and text injection boundaries.
- [ ] Add a CI build/test workflow and decide how release artifacts, native DLLs, and model files are packaged; do not check large ONNX models into the repository.

## Blocked / open questions

- The current environment has no .NET SDK installed, so package-outdated checks and a build could not be run.
- Decide whether the product must remain strictly local/offline or whether an optional cloud transcription provider is acceptable.
- Confirm target hardware beyond the current RTX/DirectML assumption: NVIDIA only, or NVIDIA/AMD/Intel plus CPU.
- Confirm desired behavior: push-to-talk clips, continuous dictation, streaming partial text, or all three.

## Audit recommendations

### P0 — Establish the baseline before changing engines

- [ ] Install the supported .NET SDK and record it in the developer prerequisites. The solution targets `net8.0`/`net8.0-windows`, but this machine currently reports no installed SDK.
- [ ] Run restore, Debug build, Release build, and a smoke test on a clean machine. Capture failures from the custom Release `CopyToLocalAppData` target and verify that the published/native runtime files are complete.
- [ ] Add a version policy: `global.json` or an explicit documented SDK band, package lock/update policy, and a repeatable dependency audit.
- [ ] Add CI for restore/build/test and a protected manual hardware benchmark job for CPU and GPU paths.

### P0 — Make the transcription model contract explicit

- [ ] Replace the generic ONNX input-name/type guessing in `WhisperEngine` with a model adapter/manifest. The current code hardcodes tensor ranks, token `50258`, 30-second padding, parameter defaults, and filename-based GPU detection.
- [ ] Define supported model families and versions, required sample rate, language/task behavior, tokenizer, preprocessing, decoding settings, output format, and execution-provider requirements.
- [ ] Add model metadata inspection and a friendly compatibility error before creating a session. Never infer GPU requirements from whether a filename contains `gpu`.
- [ ] Build a benchmark corpus from the existing prompt examples plus noisy speech, names/jargon, numbers, punctuation, long-form audio, silence, and multiple microphones. Track word error rate, latency, memory, and hallucination/duplication rate.

### P1 — Re-evaluate the inference backend

- [ ] Prototype a branch using `Whisper.net`/`whisper.cpp` with the CPU runtime and Vulkan runtime. Compare quality, startup time, memory, packaging complexity, NVIDIA performance, and cross-vendor behavior against the current ONNX/DirectML path.
- [ ] Investigate Windows ML as the longer-term Windows execution-provider layer. Microsoft now describes DirectML as legacy and Windows ML can manage CPU/DirectML plus downloadable vendor providers on supported Windows 11 24H2 systems; keep the current DirectML path until a measured replacement is proven.
- [ ] Keep `faster-whisper` as a benchmark/reference option, not the first integration candidate: its Python/CTranslate2/CUDA and cuDNN requirements would complicate this native WPF app.
- [ ] Add voice activity detection and silence trimming before decoding. Evaluate Silero VAD or equivalent, including false-start/false-stop behavior and latency, rather than padding every recording to a full 30 seconds.

### P1 — Study the Wispr Flow feature gap without copying its privacy model

- [ ] Compare WhisperPete against Wispr Flow on measurable scenarios: punctuation/auto-editing, app-specific style, custom dictionary, snippets, language switching, context awareness, latency, correction learning, and cross-device continuity.
- [ ] Decide which features belong in the local-first product: custom vocabulary, app-aware formatting, user-selectable styles, snippets, streaming feedback, and optional post-processing.
- [ ] Treat context awareness as a consented feature with clear boundaries. Wispr documents that context can include app/text/screen information and that transcription is cloud-processed; WhisperPete should default to no context capture and no network calls.
- [ ] If cloud or remote APIs are ever added, make them opt-in, disclose data flow, provide a visible offline mode, and keep provider integration behind an interface.

### P1 — Fix reliability and privacy defaults

- [ ] Make debug audio recording opt-in rather than default-on; show retention/location and provide a clear delete action. Current recordings are stored under `%LOCALAPPDATA%\\WhisperPete\\debug_recordings`.
- [ ] Replace broad silent catches in settings, logging, icon loading, and model metadata paths with structured diagnostic logging and user-safe error reporting.
- [ ] Make recording stop/cancellation robust: handle repeated hotkey presses, device removal, `RecordingStopped` errors, app shutdown during capture, and UI-dispatcher exceptions.
- [ ] Prevent error strings from being injected as dictated text; only call `TextInjector.InjectText` for a successful transcript.
- [ ] Review `TextInjector` focus timing, Unicode/keyboard layout behavior, elevated-target limitations, and clipboard fallback/security.

### P2 — Clean up project health and product readiness

- [ ] Remove or rename the stale `Class1.cs` placeholder and the old `WisprflowAlternative` namespace/migration residue after confirming no user settings still need migration.
- [ ] Centralize package versions and evaluate current stable updates in a separate compatibility change. Current pins include ONNX Runtime DirectML `1.22.2`, NAudio `2.2.1`, logging abstractions `8.0.2`, and `System.Drawing.Common` `10.0.3`; do not blindly mix major .NET package generations.
- [ ] Reconcile README/version/changelog claims with actual behavior, including repository URL, current release number, supported GPUs, model acquisition, and whether the app is truly release-ready.
- [ ] Add an installer or self-contained publish plan, signing/update strategy, crash-log collection policy, and clean uninstall behavior.
- [ ] Add accessibility and usability checks for tray-only operation, keyboard navigation, high DPI, screen readers, microphone permission/errors, and first-run model setup.

## Reference links for the audit

- [Microsoft Windows AI FAQ](https://learn.microsoft.com/en-us/windows/ai/faq)
- [Microsoft Windows ML execution providers](https://learn.microsoft.com/en-us/windows/ai/new-windows-ml/supported-execution-providers)
- [ONNX Runtime DirectML package](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.DirectML)
- [Whisper.net package and runtimes](https://www.nuget.org/packages/Whisper.net)
- [whisper.cpp capabilities](https://github.com/ggml-org/whisper.cpp)
- [Silero VAD model history](https://github.com/snakers4/silero-vad/wiki/Version-history-and-Available-Models)
- [Wispr Flow context awareness](https://docs.wisprflow.ai/articles/4678293671-Context-Awareness)
- [Wispr Flow data controls](https://wisprflow.ai/data-controls)

## Recently completed

- Added repository-relative launch guidance to the existing workflow.
- Recorded 2026 local STT library/model recommendations in `recommendations.md` (Whisper.net first, sherpa-onnx + Parakeet second, transcribe.cpp watch-only).
