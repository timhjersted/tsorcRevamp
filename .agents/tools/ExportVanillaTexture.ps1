param([Parameter(Mandatory=$true)][string]$InputPath, [Parameter(Mandatory=$true)][string]$OutputPath)
# Export installed Terraria Color-format XNB art for offline measurement; no GPU or game needed.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskFna = [Reflection.Assembly]::LoadFrom('C:/Program Files (x86)/Steam/steamapps/common/tModLoader/Libraries/FNA/1.0.0/FNA.dll')
$taskInput = [IO.File]::OpenRead($InputPath)
$taskReader = [IO.BinaryReader]::new($taskInput)
try {
    if ([Text.Encoding]::ASCII.GetString($taskReader.ReadBytes(3)) -ne 'XNB') { throw 'Not XNB' }
    $null = $taskReader.ReadByte(); $null = $taskReader.ReadByte()
    $taskFlags = $taskReader.ReadByte(); $null = $taskReader.ReadInt32()
    $taskPayload = [IO.MemoryStream]::new()
    if (($taskFlags -band 128) -ne 0) {
        $taskLength = $taskReader.ReadInt32()
        $taskType = $taskFna.GetType('Microsoft.Xna.Framework.Content.LzxDecoder')
        $taskDecoder = [Activator]::CreateInstance($taskType, [Reflection.BindingFlags]'Instance,Public,NonPublic', $null, @([int]16), $null)
        $taskMethod = $taskType.GetMethod('Decompress', [Reflection.BindingFlags]'Instance,Public,NonPublic')
        while ($taskPayload.Length -lt $taskLength) {
            $taskHi = $taskReader.ReadByte(); $taskLo = $taskReader.ReadByte()
            $taskFrame = [Math]::Min(32768, $taskLength - $taskPayload.Length)
            if ($taskHi -eq 255) {
                $taskFrame = ([int]$taskLo -shl 8) -bor $taskReader.ReadByte()
                $taskHi = $taskReader.ReadByte(); $taskLo = $taskReader.ReadByte()
            }
            $taskBlock = ([int]$taskHi -shl 8) -bor $taskLo
            $taskEnd = $taskInput.Position + $taskBlock
            $taskResult = $taskMethod.Invoke($taskDecoder, @($taskInput, [int]$taskBlock, $taskPayload, [int]$taskFrame))
            if ($taskResult -ne 0) { throw "LZX failed: $taskResult" }
            $taskInput.Position = $taskEnd
        }
    } elseif (($taskFlags -band 64) -ne 0) { throw 'LZ4 not supported' }
    else { $taskInput.CopyTo($taskPayload) }
    $taskPayload.Position = 0
    $taskData = [IO.BinaryReader]::new($taskPayload)
    $taskReaders = $taskData.Read7BitEncodedInt()
    for ($taskIndex = 0; $taskIndex -lt $taskReaders; $taskIndex++) { $null = $taskData.ReadString(); $null = $taskData.ReadInt32() }
    $null = $taskData.Read7BitEncodedInt(); $null = $taskData.Read7BitEncodedInt()
    $taskFormat = $taskData.ReadInt32(); $taskWidth = $taskData.ReadInt32(); $taskHeight = $taskData.ReadInt32()
    $null = $taskData.ReadInt32(); $taskPixelLength = $taskData.ReadInt32()
    if ($taskFormat -ne 0 -or $taskPixelLength -ne $taskWidth*$taskHeight*4) { throw 'Expected Color RGBA texture' }
    $taskPixels = $taskData.ReadBytes($taskPixelLength)
    $taskBitmap = [Drawing.Bitmap]::new($taskWidth, $taskHeight)
    for ($taskY = 0; $taskY -lt $taskHeight; $taskY++) {
        for ($taskX = 0; $taskX -lt $taskWidth; $taskX++) {
            $taskOffset = ($taskY*$taskWidth+$taskX)*4
            $taskBitmap.SetPixel($taskX, $taskY, [Drawing.Color]::FromArgb($taskPixels[$taskOffset+3], $taskPixels[$taskOffset], $taskPixels[$taskOffset+1], $taskPixels[$taskOffset+2]))
        }
    }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutputPath))) | Out-Null
    $taskBitmap.Save([IO.Path]::GetFullPath($OutputPath), [Drawing.Imaging.ImageFormat]::Png)
    $taskBitmap.Dispose(); $taskData.Dispose(); $taskPayload.Dispose()
    Write-Output "Exported ${taskWidth}x${taskHeight}: $OutputPath"
} finally { $taskReader.Dispose(); $taskInput.Dispose() }
