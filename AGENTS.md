# WhisperPete Agent Guide

## Purpose

WhisperPete is a local-first Windows tray application for speech-to-text. It captures audio with NAudio, transcribes locally with optimized Whisper ONNX models, uses ONNX Runtime with DirectML or CPU execution, and injects text into the active window.

## Scope and deliverables

This guide applies to the entire repository. Expected deliverables are maintainable C#/.NET 8 code, reliable Windows tray behavior, reproducible builds, accurate documentation, and verified release artifacts.

## Repository map

- `WhisperPete.Core/` — audio capture, transcription, settings, logging, and text injection.
- `WhisperPete.Tray/` — WPF tray shell, hotkeys, overlays, and application UI.
- `.agents/workflows/` — existing reusable local workflows for rebuilding and running the app.
- `README.md` — user-facing setup, build, and usage documentation.
- `ARCHITECTURE.md` — architecture notes and diagrams.
- `CHANGELOG.md` — release history.
- `recommendations.md` — advisory notes on transcription libraries and models; not a task list.
- `prompts.md` — concise reverse-chronological log of program-related user prompts.
- `tasks.md` — active work, handoff notes, and open questions.

## Rules for agents

- Read this file first, then read `tasks.md` when active work is being tracked.
- Keep this file concise and durable. Do not turn it into a session log, task dump, or changelog.
- Store active plans and changing tasks in `tasks.md`.
- Preserve existing user changes and inspect relevant files before editing.
- Make narrowly scoped changes and verify them before moving on.
- Ask before major structural changes, new dependencies, or changes to model/runtime behavior that could affect users.
- Do not add vendor-specific instruction files. If a runtime requires one, keep it thin and point it to this file.
- Record program-related user prompts in root-level `prompts.md` according to its prompt-traceability rules. Do not record unrelated administrative or meta prompts.

## Development conventions

- Keep reusable project utilities in `scripts/`; do not place helper scripts in the repository root unless they are true entry points.
- Keep reusable agent workflows in `skills/` when a repeatable workflow becomes substantial; existing `.agents/workflows/` files are retained for compatibility.
- Notice recurring multi-step work and propose a focused reusable skill in `skills/` when it would materially improve consistency; do not create skills for one-off tasks.
- Keep generated exports and packaged deliverables in an appropriate output folder only when a real recurring output requires one. Do not create speculative folders.
- Use clear, purpose-based, lowercase kebab-case names for new folders unless a platform or project convention requires otherwise.
- Update the nearest relevant README or index whenever documentation structure changes.
- Update `README.md`, `ARCHITECTURE.md`, or `CHANGELOG.md` when a change makes their documented behavior inaccurate.

## Build and verification

- Restore/build with the .NET 8 SDK and use `WhisperPete.sln` as the solution entry point.
- Prefer the existing rebuild workflow for a clean Release build; use Debug when investigating a specific issue.
- Verify model-path, CPU/GPU selection, hotkey, text injection, and tray behavior when changes touch those areas.
- Keep large ONNX models and build outputs out of version control as configured in `.gitignore`.

## Task and prompt handling

Use `tasks.md` for current focus, next tasks, blocked/open questions, and concise handoff notes. After completing meaningful work, update it so a fresh session can resume without relying on chat history.

Add each program-related user prompt to the top of `prompts.md` in reverse chronological order. Paraphrase it rather than copying it, and include the local timestamp, a short goal, the active computer hostname, and technical context when useful. Keep it a prompt log—not a transcript, task list, changelog, or place for secrets.

> [!NOTE]
> This project was initialized using the agentic master prompt provided by [AgenticProjectInitializer](https://github.com/pbeens/AgenticProjectInitializer/blob/main/master-prompt.md).
