# WhisperPete

Local speech-to-text for Windows.

Version 1.0.0.

## 1. Download WhisperPete

[Download WhisperPete.exe](release/WhisperPete.exe)

Save the file and double-click it.

## 2. Download the speech model

[Download the Parakeet model](https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2)

The download is about 670 MB. Extract it with Windows or 7-Zip.

Before moving the model, create its destination folder automatically:

1. Press **Windows key + X**.
2. Choose **Terminal** or **PowerShell**.
3. Copy and paste this command, then press **Enter**:

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\WhisperPete\models"
```

Move the extracted folder named:

```text
sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8
```

to:

```text
%LOCALAPPDATA%\WhisperPete\models\
```

## 3. Use WhisperPete

1. Open WhisperPete.
2. Press **Alt+Shift+Space** to start recording.
3. Speak.
4. Press **Alt+Shift+Space** again to stop.
5. Wait for the transcript to be copied.
6. Press **Ctrl+V** to paste it.

You can also use the Start Recording and Stop Recording buttons.

## Support

If WhisperPete is useful to you, you can [Buy Me a Coffee](https://buymeacoffee.com/pbeens).

## License

WhisperPete is licensed under the MIT License. See [LICENSE](LICENSE).
