---
description: Clean and Rebuild the WhisperPete Solution
---

This workflow ensures a clean, fresh build of the WhisperPete application.

> [!TIP]
> **Which version should I run?**
> Always run the **Release** version (`/bin/Release/...`). It is optimized for performance and is the intended final product. The **Debug** version is only needed if you are a developer investigating specific code errors.

// turbo

1. Stop any running WhisperPete processes to release file locks:

```powershell
Get-Process WhisperPete.Tray -ErrorAction SilentlyContinue | Stop-Process -Force
```

// turbo
2. Perform a fresh **Release** build (Recommended):

```powershell
dotnet build WhisperPete.sln -c Release --no-incremental
```

// turbo
3. Perform a fresh **Debug** build (Optional):

```powershell
dotnet build WhisperPete.sln -c Debug --no-incremental
```
