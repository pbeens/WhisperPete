# WhisperPete transcription recommendations

**Date:** 2026-08-28  
**Scope:** Local speech-to-text libraries and models that fit this .NET 8 Windows tray app.  
**Status:** Advisory only. Do not change the engine, models, or runtime until a recommendation here is explicitly chosen.

This note is a technology review, not a changelog and not a task list. Active work stays in [`tasks.md`](tasks.md).

## Current stack

WhisperPete still uses the original 0.5.0 path from early 2026:

| Layer | What ships today | Pin |
| :--- | :--- | :--- |
| Capture | NAudio `WaveInEvent`, 16 kHz mono | NAudio 2.2.1 |
| Model | Community Olive Whisper ONNX (tiny / medium, int8) | `whisper-olive` via Hugging Face |
| Runtime | ONNX Runtime + DirectML, plus `ortextensions.dll` | `Microsoft.ML.OnnxRuntime.DirectML` 1.22.2, Extensions 0.13.0 |
| Decode | Hand-built tensors in `WhisperEngine.TranscribeChunk` | Custom |
| Long audio | Pad every clip to 30 s, then chunk | "Marathon Mode" |
| Output | Unicode `SendInput` | `TextInjector` |

That design was reasonable when the repo was initialized. It is now the main quality and maintenance risk.

The engine guesses every ONNX input from name and C# type, hardcodes Whisper token `50258`, infers GPU from the filename containing `"gpu"`, and pads audio because DirectML convolution nodes were unstable on variable-length input. Prompt history already records the consequences: tensor rank/type mismatches, `ConstantOfShape` crashes past 30 seconds, last-word truncation, and tiny-model hallucinations such as "box" becoming "sponns".

Keeping that path as the only backend will fight the same class of bugs on every model upgrade.

## What changed since initialization

Local speech-to-text is no longer "Whisper ONNX or nothing."

1. **Whisper is no longer the accuracy leader for English dictation.** NVIDIA Parakeet TDT 0.6B v3 sits at the top of public Open ASR Leaderboard averages (~6.3% WER across 25 European languages) at about 600M parameters, with throughput far above Whisper large-v3. Whisper remains the best default only when you need 99-language coverage.
2. **Streaming models exist now.** Nemotron Speech / Nemotron 3.5 ASR, Moonshine Streaming, and sherpa-onnx Zipformer/transducer models can emit partial text while the user is still talking. WhisperPete is still stop-then-decode.
3. **Native C# runtimes matured.** You no longer have to hand-feed Olive graphs. Whisper.net, sherpa-onnx, and EchoSharp all expose NuGet APIs that take PCM and return text.
4. **GGUF/ggml is the portable model format.** `whisper.cpp` and the newer `transcribe.cpp` run many families from one binary with Vulkan/CUDA. The Olive all-in-one `.onnx` files this project uses are a niche export.
5. **The DirectML ONNX Runtime package lagged.** This repo pins 1.22.2. The DirectML NuGet latest is 1.24.4 (March 2026). CPU/CUDA ONNX Runtime is already at 1.29.x. Microsoft is also pushing Windows ML as the Windows execution-provider layer. Bumping DirectML in place will not fix a brittle model contract.

## Fit filter

Recommendations below assume WhisperPete stays:

- a local-first Windows tray app
- .NET / C# (no Python sidecar as the product runtime)
- hotkey dictation into the focused window
- no cloud audio by default

Python stacks such as `faster-whisper` and RealtimeSTT are useful as *benchmarks*, not as the app engine.

## Ranked options

### 1. Recommended next engine: Whisper.net (`whisper.cpp`)

- GitHub: [sandrohanea/whisper.net](https://github.com/sandrohanea/whisper.net)
- Native: [ggml-org/whisper.cpp](https://github.com/ggml-org/whisper.cpp)
- NuGet: `Whisper.net` + a runtime package, currently 1.9.1

This is the smallest-risk replacement for the current Olive session.

**Why it fits**

- Real C# API: load a GGML/GGUF model, pass 16 kHz float PCM, get segments. No tensor-rank guesswork.
- GPU without DirectML: `Whisper.net.Runtime.Cuda` / `Cuda12` for NVIDIA, `Whisper.net.Runtime.Vulkan` for a vendor-neutral Windows GPU path (AMD/Intel/NVIDIA).
- Built-in Silero VAD as of 1.9.1 (`WhisperVadFactory`), which is the right way to stop padding every clip to 30 seconds.
- English-only models (`tiny.en`, `base.en`, `small.en`, `distil-large-v3`) will beat the current multilingual tiny Olive model on this user's dictation tests.
- `ISpeechToTextClient` exists if you later want `Microsoft.Extensions.AI`.

**Trade-off**

- Still Whisper-family accuracy. For English-only quality, Parakeet is stronger (see option 2).
- CUDA runtimes need the matching NVIDIA toolkit/driver story. Vulkan is the easier Windows GPU default if you do not want to ship CUDA.

**Suggested first prototype:** CPU runtime + Vulkan runtime, `small.en` or `distil-large-v3`, Silero VAD, no 30-second padding.

### 2. Best English accuracy in C#: sherpa-onnx + Parakeet

- GitHub: [k2-fsa/sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) (~14k stars, actively released)
- NuGet: [`org.k2fsa.sherpa.onnx`](https://www.nuget.org/packages/org.k2fsa.sherpa.onnx) 1.13.5 (August 2026)
- Docs: [C# API](https://k2-fsa.github.io/sherpa/onnx/csharp-api/index.html)
- Model to try first: `sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8`

This is the strongest *product* upgrade if dictation is mostly English (or the 25 European languages Parakeet v3 covers).

**Why it fits**

- Official C# bindings and Windows native packages. Microphone and file examples already exist.
- Same local-ONNX idea as today, but with a maintained model zoo instead of one community Olive graph.
- Offline Parakeet for the final transcript; optional streaming Zipformer/Nemotron for live partials.
- Bundled Silero VAD, punctuation, and speech enhancement (GTCRN / DPDFNet).
- Moonshine Tiny/Base is also in the same zoo if you want a tiny English MIT-licensed fallback.

**Trade-offs**

- Parakeet TDT 0.6B v3 is CC-BY-4.0. Attribution is required; that is acceptable for a local app but must be documented.
- Language coverage is 25 European languages, not Whisper's 99.
- One RealtimeSTT note flagged intermittent empty Parakeet finals on native Windows in *their* Python binding. That is not a sherpa-onnx C# finding, but the prototype must treat empty decode as a failure, not as silence.
- GPU on Windows is less turnkey than Whisper.net Vulkan/CUDA. Confirm the Windows provider (CPU INT8 is already fast enough for dictation on an RTX-class machine).

**Suggested second prototype:** VAD → Parakeet TDT 0.6B v3 INT8 for the final paste. Keep Whisper.net as the multilingual fallback.

### 3. Watch, do not adopt yet: transcribe.cpp

- GitHub: [handy-computer/transcribe.cpp](https://github.com/handy-computer/transcribe.cpp)
- Related: [mudler/parakeet.cpp](https://github.com/mudler/parakeet.cpp)

This is the 2026 "one runtime, many families" project: Parakeet, Canary, Whisper, Moonshine, Nemotron streaming, Voxtral, SenseVoice, Granite Speech, and more, all as GGUF, with Vulkan/CUDA/CPU.

**Why it matters**

- If it stays healthy, it is the long-term replacement for both Whisper.net *and* sherpa-onnx.
- It is the only native stack that can swap Whisper ↔ Parakeet ↔ Moonshine without changing the app's audio/UI layer.

**Why not now**

- Official bindings are Python, TypeScript, Rust, and Swift. There is no first-party C# package.
- `TranscribeCppSharp` exists on NuGet as a 0.1.3 *preview* (August 2026). Too young to ship in a tray app.
- Replacing WhisperPete's engine with a preview FFI wrapper would be a larger bet than Whisper.net or sherpa-onnx.

Revisit in a later session. If a stable C# NuGet appears, it becomes the preferred long-term runtime.

### 4. Keep the current Olive / DirectML path only as a compatibility backend

Do not invest further in hand-rolled Olive Whisper graphs.

If you keep it at all:

- Hide it behind an `ISpeechEngine` interface.
- Stop inferring device from the filename.
- Do not bump `Microsoft.ML.OnnxRuntime.DirectML` from 1.22.2 to 1.24.4 without replaying the existing debug WAVs. Olive custom ops plus DirectML is exactly where the old rank/type crashes came from.
- Prefer a published, versioned model card over `thewh1teagle/whisper-olive` as an unpinned download.

Windows ML is a plausible *execution provider* later, not an STT library. It does not replace Whisper/Parakeet.

### 5. Not recommended as the app engine

| Project | Why skip as the runtime |
| :--- | :--- |
| [KoljaB/RealtimeSTT](https://github.com/KoljaB/RealtimeSTT) | Excellent Python orchestrator. Wrong language for this WPF process. |
| `faster-whisper` / CTranslate2 | Strong benchmark, CUDA/cuDNN packaging is hostile to a tray installer. |
| [OpenWhispr](https://github.com/OpenWhispr/openwhispr) | Feature-rich Electron dictation app (Parakeet + Whisper). A product competitor, not a library. |
| Azure / OpenAI cloud STT | Conflicts with the local-first promise unless added as an explicit opt-in later. |
| EchoSharp as a hard dependency | Useful *reference* ([sandrohanea/echosharp](https://github.com/sandrohanea/echosharp)) for VAD + Whisper.net + sherpa wiring. Do not take a kitchen-sink audio framework just to transcribe one hotkey clip. |

## Models to actually try

Assume English-first dictation on an RTX-class Windows PC.

| Priority | Model | Runtime | Why |
| :--- | :--- | :--- | :--- |
| Default candidate | NVIDIA Parakeet TDT 0.6B v3 INT8 | sherpa-onnx | Best accuracy/speed for EN + major European languages. Punctuation and capitalization included. |
| Whisper quality | distil-large-v3 or large-v3-turbo | Whisper.net | Stays in the Whisper family, much better than tiny/medium Olive, still MIT. |
| Fast Whisper | `small.en` | Whisper.net | Honest replacement for the current tiny model. |
| Tiny / CPU | Moonshine Tiny or Base | sherpa-onnx | MIT, English, low hallucination, small disk. |
| Streaming later | Nemotron 3.5 ASR Streaming or Moonshine Streaming | sherpa-onnx or transcribe.cpp | Only after push-to-talk quality is solid. |
| Avoid as default | Olive Whisper tiny multilingual | current engine | Already shown to hallucinate on this machine's test phrases. |

Do not check model files into git. `.gitignore` already blocks `*.onnx`; the working tree still has local Olive files. Keep downloads under `%LOCALAPPDATA%\WhisperPete\models` with checksums.

## Adjacent library updates (worth doing with any engine)

These are independent of the model choice and would help the current app.

1. **Voice activity detection.** Add Silero VAD before decode. Options: Whisper.net 1.9.1 VAD, sherpa-onnx VAD, or a small ONNX Silero session. This should replace 30-second silence padding and reduce "of the" loops on quiet tails.
2. **Capture path.** NAudio 2.2.1 → 2.3.0 is a conservative bump (March 2026). NAudio 3.0.1 (August 2026) adds `WasapiRecorder` with `IAudioClient3` low-latency capture. Prefer WASAPI over `WaveInEvent` once 3.x is evaluated; do not jump 2.2.1 → 3.0.1 in the same change as an engine swap.
3. **Stop injecting errors.** `StopRecordingAsync` currently pastes strings like `Error: Audio clip too short` into the focused window. Only inject a successful transcript.
4. **English model + initial prompt.** The Quick Brown Fox test failed on numbers, street names, and quotes. An English-only model plus a dictation prompt ("comma, period, new line") will buy more accuracy than another Olive quantization.
5. **Package hygiene, separate from STT.** `System.Drawing.Common` 10.0.3 on a `net8.0-windows` project mixes a .NET 10 package into a .NET 8 app. Logging abstractions are 8.0.2. Centralize versions when you next touch the csproj files.

## Suggested decision

Do **not** rewrite the tray/hotkey/overlay/injection shell.

Do introduce a small engine interface in `WhisperPete.Core`, then run two measured prototypes on the existing debug recordings plus the Quick Brown Fox prompt:

1. **Whisper.net + Vulkan + `small.en` + Silero VAD** — lowest integration risk, drops the Olive tensor layer.
2. **sherpa-onnx + Parakeet TDT 0.6B v3 INT8 + Silero VAD** — likely best English dictation quality.

Keep the current DirectML/Olive path behind the same interface until one prototype wins on WER, latency, memory, and packaging. Treat transcribe.cpp as the 2026/2027 consolidation target if/when C# bindings stabilize.

## Reference links

- [Whisper.net](https://github.com/sandrohanea/whisper.net)
- [whisper.cpp](https://github.com/ggml-org/whisper.cpp)
- [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)
- [sherpa-onnx C# API](https://k2-fsa.github.io/sherpa/onnx/csharp-api/index.html)
- [org.k2fsa.sherpa.onnx on NuGet](https://www.nuget.org/packages/org.k2fsa.sherpa.onnx)
- [NVIDIA Parakeet TDT 0.6B v3](https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3)
- [transcribe.cpp](https://github.com/handy-computer/transcribe.cpp)
- [EchoSharp](https://github.com/sandrohanea/echosharp)
- [Silero VAD](https://github.com/snakers4/silero-vad)
- [NAudio 3.0](https://github.com/naudio/NAudio)
- [ONNX Runtime DirectML 1.24.4](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime.DirectML)
- [Windows ML execution providers](https://learn.microsoft.com/en-us/windows/ai/new-windows-ml/supported-execution-providers)
