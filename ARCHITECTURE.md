# WhisperPete Architecture

This document describes the core architecture of WhisperPete using Mermaid diagrams for native rendering.

## System Flow

```mermaid
flowchart LR
    subgraph "Input Layer"
        A["Audio Input (NAudio)"]
        B["Audio Buffer"]
    end

    subgraph "Processing Layer (AI)"
        C["ONNX Engine (DirectML)"]
        D["Whisper Model (Int8)"]
    end

    subgraph "Output Layer"
        E["Text Injection (SendInput)"]
    end

    A --> B
    B --> C
    D -.-> C
    C --> E

    style C fill:#f9f,stroke:#333,stroke-width:2px
    style E fill:#bfb,stroke:#333,stroke-width:2px
```

## Description

1. **Input Layer**: Captures raw audio via `NAudio` and buffers it for processing.
2. **Processing Layer**: The heart of the app. It uses `ONNX Runtime` with `DirectML` to leverage RTX GPUs for high-speed transcription.
3. **Output Layer**: Once transcribed, the text is programmatically injected into the active window using the Windows `SendInput` API.
