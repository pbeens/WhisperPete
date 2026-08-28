Add-Type -AssemblyName System.Drawing
$pngPath = "d:\My Documents\GitHub\Wisprflow-ALternative\WhisperPete.Tray\app_icon.png"
$icoPath = "d:\My Documents\GitHub\Wisprflow-ALternative\WhisperPete.Tray\app_icon.ico"

$bmp = [System.Drawing.Bitmap]::FromFile($pngPath)
$iconHandle = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($iconHandle)

$fileStream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
$icon.Save($fileStream)
$fileStream.Close()
$icon.Dispose()
$bmp.Dispose()
