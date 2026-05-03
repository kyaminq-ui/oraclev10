# Extracts specific GIFs into OracleHUD/Frames (same layout as OracleMajGifImportTool)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

# Repo root = parent of Tools/ when this script sits in Tools/
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$srcRoot = Join-Path $projectRoot "mise_a_jour_animation\mana_pm_animation"
$outRoot = Join-Path $projectRoot "Assets\_Game\Resources\OracleHUD\GifSource"
$framesRoot = Join-Path $projectRoot "Assets\_Game\Resources\OracleHUD\Frames"
$gifs = @("end_turn_animated_button.gif", "animated_hp_icon.gif")

New-Item -ItemType Directory -Force -Path $outRoot | Out-Null
New-Item -ItemType Directory -Force -Path $framesRoot | Out-Null

foreach ($name in $gifs) {
    $from = Join-Path $srcRoot $name
    if (-not (Test-Path $from)) { throw "Missing: $from" }
    Copy-Item -LiteralPath $from -Destination $outRoot -Force
    $base = [IO.Path]::GetFileNameWithoutExtension($name)
    $folder = Join-Path $framesRoot $base
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $gif = [System.Drawing.Image]::FromFile((Resolve-Path $from).Path)
    try {
        $fd = New-Object System.Drawing.Imaging.FrameDimension($gif.FrameDimensionsList[0])
        $cnt = $gif.GetFrameCount($fd)
        for ($i = 0; $i -lt $cnt; $i++) {
            [void]$gif.SelectActiveFrame($fd, $i)
            $bmp = New-Object System.Drawing.Bitmap $gif.Width, $gif.Height
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            [void]$g.DrawImage($gif, 0, 0)
            $suffix = "{0:D3}" -f $i
            $png = Join-Path $folder "${base}_${suffix}.png"
            $bmp.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
            $g.Dispose()
            $bmp.Dispose()
        }
        Write-Host "Extracted $base : $cnt frames -> $folder"
    }
    finally {
        $gif.Dispose()
    }
}

Write-Host "Done."
