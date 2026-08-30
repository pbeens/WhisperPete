# prompts.md

## 2026-08-30T13:08:58-04:00 — Correct model archive instructions

- **Goal:** Make the model download instructions accurately explain `.tar.bz2` extraction on Windows.
- **Prompt summary:** Reported that Windows does not automatically extract the Parakeet BZ2 archive.
- **Computer:** XPS-8950
- **Technical context:** README now directs users to 7-Zip and explains the two extraction steps needed to unpack the `.tar.bz2` archive.

## 2026-08-30T13:05:52-04:00 — Simplify model-folder setup

- **Goal:** Make the README automatically create the model destination folder for first-time users.
- **Prompt summary:** Reported that the documented model path does not exist on a new machine and asked for an automated way to create it.
- **Computer:** XPS-8950
- **Technical context:** Added a copy-and-paste PowerShell command that creates `%LOCALAPPDATA%\WhisperPete\models` before the Parakeet folder is moved there.

## 2026-08-30T12:36:42-04:00 — Retain only public-release prompt history

- **Goal:** Remove prompts from before the current public release work.
- **Prompt summary:** Requested deletion of old prompts so this file covers only the 1.0.0 release.
- **Computer:** XPS-8950
- **Technical context:** The active product is WhisperPete 1.0.0; earlier migration, prototype, and historical prompts were removed.

## 2026-08-30T12:36:08-04:00 — Remove obsolete planning history

- **Goal:** Delete obsolete recommendation material and reduce task tracking to 1.0.0 work.
- **Prompt summary:** Requested removal of old-version references and legacy migration tasks from the repository documentation.
- **Computer:** XPS-8950
- **Technical context:** `recommendations.md` was deleted and `tasks.md` was rewritten around 1.0.0 release validation.

## 2026-08-30T12:28:45-04:00 — Add Buy Me a Coffee support link

- **Goal:** Add the user’s Buy Me a Coffee link to the application popup and README.
- **Prompt summary:** Supplied `https://buymeacoffee.com/pbeens` for the public release.
- **Computer:** XPS-8950
- **Technical context:** The Rust/Tauri popup and README contain the support link; the release is version 1.0.0.

## 2026-08-30T12:13:18-04:00 — Prepare the public release

- **Goal:** Prepare the first public Windows release with a downloadable executable and simple model setup instructions.
- **Prompt summary:** Requested removal of obsolete implementation files, a direct executable download, and straightforward Parakeet model instructions.
- **Computer:** XPS-8950
- **Technical context:** The active product is the Rust/Tauri application, staged as `release/WhisperPete.exe`.
