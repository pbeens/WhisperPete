# Changelog

All notable changes to **WhisperPete** will be documented in this file.

## [0.5.0] - 2026-02-25

### Added

- **Marathon Mode**: Automated 30-second audio chunking to support indefinitely long recordings.
- **Branding**: Full transition to **WhisperPete** identity with custom cyan soundwave icons.
- **Hardware Insights**: Real-time "Compute Device" detection (DirectML GPU vs CPU).
- **Persistent Storage**: Migrated settings and logs to `%LOCALAPPDATA%` for build resilience.
- **Advanced Options**: Startup integration (Registry) and Debug Recording control.
- **UI Overlay**: heads-up display with hotkey instructions (`Ctrl+Alt+W to stop`).

### Fixed

- **Icon Startup Error**: Resolve "Argument picture" crash with robust multi-level icon loading.
- **Persistence Race Condition**: Fixed initialization guard to prevent UI defaults from overwriting settings.
- **Global Stability**: Implemented top-level crash protection and hardened transcription lifecycle.
- **Looping/Truncation**: Fixed "of the" repetition and last-word cutoff issues.

### Optimized

- **DirectML Fallback**: Improved robustness with automatic GPU-to-CPU fallback logic.
- **Wait Timing**: Fine-tuned audio buffer flushing to prevent truncation of the last spoken words.
