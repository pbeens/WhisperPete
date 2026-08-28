# prompts.md

This file stores prompts used for development.

## 2026-08-28T16:55:22-04:00 — Retrofit agentic project guidance

- **Goal:** Bring the existing repository into alignment with the vendor-neutral agentic initializer and replace the legacy Gemini instruction file with `AGENTS.md`.
- **Prompt summary:** Requested a retrospective setup audit, missing initializer pieces, and updates to all records related to the instruction-file rename.
- **Computer:** XPS-8950
- **Technical context:** Existing .NET 8 Windows tray solution with `.agents/workflows/`, an uppercase prompt log, and a legacy vendor-specific instruction file.

## Initial System Design Prompt

"You are a senior Windows software architect and AI engineer. Help me design and implement a Windows-based alternative to WhisperFlow."

## Tensor Rank & Data Type Fixes (Olive Model)

"Transcription Error: [ErrorCode:InvalidArgument] Tensor element data type discovered: Float metadata expected: UInt8"

"Transcription Error: [ErrorCode:InvalidArgument] Invalid rank for input: max_length Got: 2 Expected: 1 Please fix either the inputs/outputs or the model."

"Transcription Error: [ErrorCode:InvalidArgument] Invalid rank for input: length_penalty Got: 2 Expected: 1 Please fix either the inputs/outputs or the model."

## Headless Startup & Background Stability

"Debugging ONNX Runtime Errors: resolve the ConstantOfShape ONNX Runtime error that occurs when the application runs without the settings window open."

"Okay, this is the test without opening the settings box. (Note: It didn't give me the last paragraph I spoke)"

## Quality Tuning & Diagnostics

"Houston, we have a problem! Repetitive 'of the' output from 3 sentences spoken (headless)."

"Is there a debug mode we can use where it saves the audio so you can analyze that afterwards?"

"Output truncated: ', I'm not' (There's much more in the audio)"

## Long-Form Audio Crash (30s+ Limit)

"Transcription Error: [ErrorCode:RuntimeException] Non-zero status code returned while running ConstantOfShape node. Name:'/ConstantOfShape' ... Tensor shape.Size() must be >= 0"

## Minor Misidentification / Hallucination

"With the settings box open it was almost perfect. The last word it got incorrectly. 'This is a test using the setting sponns.' (should be 'box' not 'sponns')"

## Marathon Mode (Chunking) Success

"This is a test using the new marathon mode. I think it called it. So I'll just read a few paragraphs, acknowledged I've added the feedback to prompts.md to keep track of the models accent regarding the misspelling of box as SPO and NS since using the tiny model location, the hallucinate sort of misinterpreted, so that's a pretty simple example. Especially on the CPU followed by"

## Accuracy Benchmark (Quick Brown Fox)

**Input Text**: "The quick brown fox jumps over thirteen lazy dogs while a distant clock chimes at 7:45 p.m. She said, “Please deliver 42 blue folders to 125 Market Street before Friday,” and then paused for three seconds. In 2026, researchers reported a 12.5 percent increase in efficiency across teams in New York, London, and Tokyo. This sentence includes commas, quotation marks, numbers, proper nouns, and varied pacing to evaluate clarity and accuracy."

**Output Text**: "The quick-ground fox jumps over 13 lazy dogs while a distant clock chimed at 745 pm. She said, "Please deliver 42 blue folders to 125 mark and street before Friday." And then paused for three seconds. In 2026, researchers reported a 12.5% increase in efficiency across teams in New York London and Tokyo. The sentence includes commas, quotation marks, numbers, property I'm very happy to evaluate clarity and accuracy."
