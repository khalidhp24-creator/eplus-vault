$ErrorActionPreference = "Stop"
$OutDir = "c:\Users\K\Desktop\Work2\MediaFireSite\SystemToolkit"
Add-Type -AssemblyName System.Drawing

function New-ToolkitIconFile([string]$IconPath) {
    $sizes = @(16, 32, 48, 256)
    $pngDataList = New-Object System.Collections.Generic.List[byte[]]

    foreach ($sz in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)

        $pad = [float]([Math]::Max(1, $sz * 0.06))
        $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($sz - 2 * $pad), ($sz - 2 * $pad))

        # Royal Blue gradient shield/badge with Crimson Red accent border
        $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $rect,
            [System.Drawing.Color]::FromArgb(255, 37, 99, 235),
            [System.Drawing.Color]::FromArgb(255, 30, 58, 138),
            45.0
        )
        $g.FillEllipse($bgBrush, $rect)

        $penWidth = [float]([Math]::Max(1.5, $sz * 0.075))
        $ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 220, 38, 38), $penWidth)
        $g.DrawEllipse($ringPen, $rect)

        # Inner White Gear teeth + ring
        $center = [float]($sz / 2.0)
        $rOuter = [float]($sz * 0.26)
        $rInner = [float]($sz * 0.14)
        $gearPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float]([Math]::Max(1.2, $sz * 0.065)))
        $g.DrawEllipse($gearPen, ($center - $rOuter), ($center - $rOuter), ($rOuter * 2), ($rOuter * 2))

        for ($a = 0; $a -lt 360; $a += 45) {
            $rad = $a * [Math]::PI / 180.0
            $x1 = $center + [float]([Math]::Cos($rad) * $rOuter)
            $y1 = $center + [float]([Math]::Sin($rad) * $rOuter)
            $x2 = $center + [float]([Math]::Cos($rad) * ($rOuter + $sz * 0.08))
            $y2 = $center + [float]([Math]::Sin($rad) * ($rOuter + $sz * 0.08))
            $g.DrawLine($gearPen, $x1, $y1, $x2, $y2)
        }

        # Crimson Red Core Hub
        $coreBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 220, 38, 38))
        $g.FillEllipse($coreBrush, ($center - $rInner), ($center - $rInner), ($rInner * 2), ($rInner * 2))

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
New-ToolkitIconFile -IconPath $IconPath

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

$CsPath = Join-Path $OutDir "SystemToolkit.cs"
$ManifestPath = Join-Path $OutDir "app.manifest"
$ExePath = Join-Path $OutDir "SystemToolkit.exe"

& $csc /nologo /target:winexe /platform:anycpu /optimize+ `
    "/out:$ExePath" `
    "/win32icon:$IconPath" `
    "/win32manifest:$ManifestPath" `
    "/r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll" `
    "$CsPath"

Copy-Item $ExePath "c:\Users\K\Desktop\Work2\MediaFireSite\SystemToolkit.exe" -Force
Write-Host "Build succeeded: $ExePath"
