$ErrorActionPreference = "Stop"
$OutDir = "c:\Users\K\Desktop\Work2\MediaFireSite\NetworkResetTool"
Add-Type -AssemblyName System.Drawing

function New-NetworkIconFile([string]$IconPath) {
    $sizes = @(16, 32, 48, 256)
    $pngDataList = New-Object System.Collections.Generic.List[byte[]]

    foreach ($sz in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)

        $pad = [float]([Math]::Max(1, $sz * 0.06))
        $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($sz - 2 * $pad), ($sz - 2 * $pad))

        $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $rect,
            [System.Drawing.Color]::FromArgb(255, 14, 116, 144),
            [System.Drawing.Color]::FromArgb(255, 30, 58, 138),
            45.0
        )
        $g.FillEllipse($bgBrush, $rect)

        $penWidth = [float]([Math]::Max(1.5, $sz * 0.07))
        $ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 56, 189, 248), $penWidth)
        $g.DrawEllipse($ringPen, $rect)

        $innerPad = [float]($sz * 0.22)
        $globeRect = New-Object System.Drawing.RectangleF($innerPad, $innerPad, ($sz - 2 * $innerPad), ($sz - 2 * $innerPad))
        $gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 255, 255, 255), [float]([Math]::Max(1.0, $sz * 0.055)))

        $g.DrawEllipse($gridPen, $globeRect)

        $vPad = [float]($sz * 0.34)
        $vRect = New-Object System.Drawing.RectangleF($vPad, $innerPad, ($sz - 2 * $vPad), ($sz - 2 * $innerPad))
        if ($vRect.Width -gt 1) {
            $g.DrawEllipse($gridPen, $vRect)
        }

        $mid = [float]($sz / 2.0)
        $g.DrawLine($gridPen, $innerPad, $mid, ($sz - $innerPad), $mid)
        $g.DrawLine($gridPen, $mid, $innerPad, $mid, ($sz - $innerPad))

        $dotSize = [float]([Math]::Max(4, $sz * 0.26))
        $dotRect = New-Object System.Drawing.RectangleF(($sz - $dotSize - $pad), ($sz - $dotSize - $pad), $dotSize, $dotSize)
        $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 34, 197, 94))
        $dotBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 15, 23, 42), [float]([Math]::Max(1.0, $sz * 0.04)))
        $g.FillEllipse($dotBrush, $dotRect)
        $g.DrawEllipse($dotBorder, $dotRect)

        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngDataList.Add($ms.ToArray())

        $ms.Dispose()
        $g.Dispose()
        $bmp.Dispose()
    }

    $fs = [System.IO.File]::Create($IconPath)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$sizes.Count)

    $offset = 6 + (16 * $sizes.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]
        $bSz = if ($s -eq 256) { [byte]0 } else { [byte]$s }
        $data = $pngDataList[$i]
        $bw.Write($bSz)
        $bw.Write($bSz)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$data.Length)
        $bw.Write([uint32]$offset)
        $offset += $data.Length
    }

    foreach ($data in $pngDataList) {
        $bw.Write($data)
    }

    $bw.Flush()
    $bw.Dispose()
    $fs.Dispose()
}

$IconPath = Join-Path $OutDir "app.ico"
New-NetworkIconFile -IconPath $IconPath

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

$CsPath = Join-Path $OutDir "NetworkResetTool.cs"
$ManifestPath = Join-Path $OutDir "app.manifest"
$ExePath = Join-Path $OutDir "NetworkResetTool.exe"

& $csc /nologo /target:winexe /platform:anycpu /optimize+ `
    "/out:$ExePath" `
    "/win32icon:$IconPath" `
    "/win32manifest:$ManifestPath" `
    "/r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll" `
    "$CsPath"

Copy-Item $ExePath "c:\Users\K\Desktop\Work2\MediaFireSite\NetworkResetTool.exe" -Force
Write-Host "Build succeeded: $ExePath"
