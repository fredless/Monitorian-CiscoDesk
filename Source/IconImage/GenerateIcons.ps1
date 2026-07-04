# Generates AppIcon.ico, DarkTrayIcon.ico and LightTrayIcon.ico for the Cisco Desk fork.
# The design keeps Monitorian's eight brightness rays but replaces the monitor rectangle
# in the center with five rounded bars (two-tower profile) evoking the Cisco mark.
#
# Run with Windows PowerShell (STA):
#   powershell.exe -Sta -NoProfile -ExecutionPolicy Bypass -File GenerateIcons.ps1

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function New-IconBitmap
{
    param(
        [int]$Size,
        [System.Windows.Media.Color]$GlyphColor,
        [bool]$DrawBackground
    )

    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()

    $scale = $Size / 256.0
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform($scale, $scale)))

    $glyphBrush = New-Object System.Windows.Media.SolidColorBrush($GlyphColor)
    $glyphBrush.Freeze()

    if ($DrawBackground)
    {
        $backBrush = New-Object System.Windows.Media.SolidColorBrush(
            [System.Windows.Media.Color]::FromRgb(0x26, 0x26, 0x26))
        $backBrush.Freeze()
        $dc.DrawRoundedRectangle($backBrush, $null,
            (New-Object System.Windows.Rect(0, 0, 256, 256)), 28, 28)
    }

    # Eight brightness rays around the center (outer radius 116). The east and west rays
    # start farther out to keep clear of the bars row which extends horizontally.
    $pen = New-Object System.Windows.Media.Pen($glyphBrush, 16)
    $pen.Freeze()
    for ($i = 0; $i -lt 8; $i++)
    {
        $angle = $i * [Math]::PI / 4
        $sin = [Math]::Sin($angle)
        $cos = [Math]::Cos($angle)
        if (($i -eq 2) -or ($i -eq 6)) { $inner = 84 } else { $inner = 72 }
        $p1 = New-Object System.Windows.Point((128 + $inner * $sin), (128 - $inner * $cos))
        $p2 = New-Object System.Windows.Point((128 + 116 * $sin), (128 - 116 * $cos))
        $dc.DrawLine($pen, $p1, $p2)
    }

    # Five rounded bars (short, tall, middle, tall, short), vertically centered.
    $heights = @(44, 80, 60, 80, 44)
    for ($k = 0; $k -lt 5; $k++)
    {
        $centerX = 128 + ($k - 2) * 28
        $h = $heights[$k]
        $rect = New-Object System.Windows.Rect(($centerX - 9), (128 - $h / 2), 18, $h)
        $dc.DrawRoundedRectangle($glyphBrush, $null, $rect, 9, 9)
    }

    $dc.Pop()
    $dc.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
        $Size, $Size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    return $bitmap
}

function Get-PngBytes
{
    param([System.Windows.Media.Imaging.BitmapSource]$Bitmap)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($Bitmap))
    $stream = New-Object System.IO.MemoryStream
    $encoder.Save($stream)
    return $stream.ToArray()
}

function Get-BmpEntryBytes
{
    param([System.Windows.Media.Imaging.BitmapSource]$Bitmap)

    # 32 bit BGRA (straight alpha) bottom-up DIB followed by an empty AND mask.
    $converted = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap(
        $Bitmap, [System.Windows.Media.PixelFormats]::Bgra32, $null, 0)

    $w = $converted.PixelWidth
    $h = $converted.PixelHeight
    $stride = $w * 4
    $pixels = New-Object byte[] ($stride * $h)
    $converted.CopyPixels($pixels, $stride, 0)

    $maskStride = [int](([Math]::Floor(($w + 31) / 32)) * 4)
    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)

    $writer.Write([int]40)            # biSize
    $writer.Write([int]$w)            # biWidth
    $writer.Write([int]($h * 2))      # biHeight (XOR + AND)
    $writer.Write([int16]1)           # biPlanes
    $writer.Write([int16]32)          # biBitCount
    $writer.Write([int]0)             # biCompression
    $writer.Write([int]($stride * $h + $maskStride * $h)) # biSizeImage
    $writer.Write([int]0); $writer.Write([int]0)
    $writer.Write([int]0); $writer.Write([int]0)

    for ($y = $h - 1; $y -ge 0; $y--)
    {
        $writer.Write($pixels, $y * $stride, $stride)
    }
    $writer.Write((New-Object byte[] ($maskStride * $h)))

    $writer.Flush()
    return $stream.ToArray()
}

function Write-Icon
{
    param(
        [string]$Path,
        [int[]]$Sizes,
        [System.Windows.Media.Color]$GlyphColor,
        [bool]$DrawBackground
    )

    $entries = @()
    foreach ($size in $Sizes)
    {
        $bitmap = New-IconBitmap -Size $size -GlyphColor $GlyphColor -DrawBackground $DrawBackground
        if ($size -ge 256)
        {
            $bytes = Get-PngBytes -Bitmap $bitmap
        }
        else
        {
            $bytes = Get-BmpEntryBytes -Bitmap $bitmap
        }
        $entries += ,@($size, $bytes)
    }

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)

    $writer.Write([int16]0)                 # reserved
    $writer.Write([int16]1)                 # type: icon
    $writer.Write([int16]$entries.Count)    # count

    $offset = 6 + 16 * $entries.Count
    foreach ($entry in $entries)
    {
        $size = $entry[0]
        $bytes = $entry[1]
        if ($size -ge 256) { $dim = 0 } else { $dim = $size }
        $writer.Write([byte]$dim)            # width
        $writer.Write([byte]$dim)            # height
        $writer.Write([byte]0)               # colors
        $writer.Write([byte]0)               # reserved
        $writer.Write([int16]1)              # planes
        $writer.Write([int16]32)             # bit count
        $writer.Write([int]$bytes.Length)    # bytes in resource
        $writer.Write([int]$offset)          # offset
        $offset += $bytes.Length
    }
    foreach ($entry in $entries)
    {
        $writer.Write([byte[]]$entry[1])
    }

    $writer.Flush()
    [System.IO.File]::WriteAllBytes($Path, $stream.ToArray())
    Write-Host "Written: $Path ($($entries.Count) entries)"
}

$white = [System.Windows.Media.Colors]::White
$black = [System.Windows.Media.Colors]::Black

# Application icon: white glyph on dark rounded square
Write-Icon -Path (Join-Path $repoRoot 'Source\Monitorian\Resources\Icons\AppIcon.ico') `
    -Sizes @(256, 48, 32, 24, 16) -GlyphColor $white -DrawBackground $true

# Tray icon for dark theme: white glyph, transparent background
Write-Icon -Path (Join-Path $repoRoot 'Source\Monitorian.Core\Resources\Icons\DarkTrayIcon.ico') `
    -Sizes @(256, 48, 40, 32, 24, 16) -GlyphColor $white -DrawBackground $false

# Tray icon for light theme: black glyph, transparent background
Write-Icon -Path (Join-Path $repoRoot 'Source\Monitorian.Core\Resources\Icons\LightTrayIcon.ico') `
    -Sizes @(256, 48, 40, 32, 24, 16) -GlyphColor $black -DrawBackground $false

# Preview PNG for inspection
$preview = New-IconBitmap -Size 256 -GlyphColor $white -DrawBackground $true
$previewBytes = Get-PngBytes -Bitmap $preview
[System.IO.File]::WriteAllBytes((Join-Path $env:TEMP 'monitorian-cisco-icon-preview.png'), $previewBytes)
Write-Host "Preview: $env:TEMP\monitorian-cisco-icon-preview.png"
