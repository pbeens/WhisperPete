# WhisperPete Agent Guide

## Purpose

WhisperPete is a local-first Windows tray application for speech-to-text. The active implementation is a Rust/Tauri application that captures audio with `cpal`, transcribes locally with sherpa-onnx and Parakeet, and copies successful transcripts to the Windows clipboard.

## Repository map

- `whisperpete-rs/` — active Rust/Tauri application, audio capture, transcription, tray shell, hotkey, UI, and clipboard output.
- `release/` — staged release artifacts intended for download.
- `README.md` — user-facing setup and usage documentation.
- `ARCHITECTURE.md` — technical design notes for maintainers.
- `CHANGELOG.md` — public release information.
- `prompts.md` — concise reverse-chronological log of program-related user prompts.
- `tasks.md` — current release tasks and handoff notes.

## Rules for agents

- Read this file first, then read `tasks.md` when active work is being tracked.
- Keep this file concise and durable. Do not turn it into a session log, task dump, or changelog.
- Store active plans and changing tasks in `tasks.md`.
- Preserve existing user changes and inspect relevant files before editing.
- Make narrowly scoped changes and verify them before moving on.
- Ask before major structural changes, new dependencies, or changes to model/runtime behavior that could affect users.
- Record program-related user prompts in `prompts.md` according to its prompt-traceability rules. Do not record unrelated administrative or meta prompts.

## Development conventions

- Keep reusable project utilities in `scripts/`.
- Keep reusable agent workflows in `skills/` when a repeatable workflow becomes substantial.
- Use clear, purpose-based, lowercase kebab-case names for new folders unless a platform or project convention requires otherwise.
- Update the nearest relevant README or index whenever documentation structure changes.
- Update `README.md`, `ARCHITECTURE.md`, or `CHANGELOG.md` when a change makes their documented behavior inaccurate.

## Build and verification

- Build the active application with the stable Rust MSVC toolchain from `whisperpete-rs/`.
- Use `cargo fmt --check`, `cargo check`, and `cargo build --release` for verification.
- Verify model-path, CPU inference, hotkey, clipboard output, and tray/window behavior when changes touch those areas.
- Keep large ONNX models and build outputs out of version control as configured in `.gitignore`.

## Task and prompt handling

Use `tasks.md` for current release work, open validation items, and concise handoff notes. After completing meaningful work, update it so a fresh session can resume without relying on chat history.

Add each program-related user prompt to the top of `prompts.md` in reverse chronological order. Paraphrase it rather than copying it, and include the local timestamp, a short goal, the active computer hostname, and technical context when useful. Keep it a prompt log—not a transcript, task list, changelog, or place for secrets.
