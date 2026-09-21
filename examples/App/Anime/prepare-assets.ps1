# Reproduce the demo crops from the screenshots supplied with the issue.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$destination = Join-Path $PSScriptRoot 'Anime/Assets'

function Export-Crop($sourceName, $name, $x, $y, $width, $height) {
    $source = [Drawing.Bitmap]::FromFile((Join-Path $repo "issues/example-anime/$sourceName"))
    try {
        # Coordinates use the reference at 424 pixels wide.
        $scale = $source.Width / 424.0
        $rectangle = [Drawing.Rectangle]::new($x * $scale, $y * $scale, $width * $scale, $height * $scale)
        $crop = $source.Clone($rectangle, $source.PixelFormat)
        try { $crop.Save((Join-Path $destination $name), [Drawing.Imaging.ImageFormat]::Jpeg) }
        finally { $crop.Dispose() }
    }
    finally { $source.Dispose() }
}

for ($row = 0; $row -lt 2; $row++) {
    for ($column = 0; $column -lt 3; $column++) {
        $index = $row * 3 + $column + 1
        Export-Crop 'Screenshot_Home_List.jpg' "poster-$index.jpg" (11 + $column * 137) (295 + $row * 214) 126 170
        Export-Crop 'Screenshot_Home_Movie.jpg' "poster-$($index + 6).jpg" (11 + $column * 137) (295 + $row * 214) 126 170
    }
}
Export-Crop 'Screenshot_Home.jpg' 'banner.jpg' 11 157 400 196
Export-Crop 'Screenshot_User.jpg' 'avatar.jpg' 20 85 50 50
Export-Crop 'Screenshot_User_History.jpg' 'bookworm.jpg' 13 105 150 86
Export-Crop 'Screenshot_User_Favorite.jpg' 'rabbit.jpg' 13 105 150 86

# Use separate aspect ratios for the poster grid (portrait) and library rows (landscape).
function Export-AspectCrop($sourcePath, $targetPath, $ratio) {
    $source = [Drawing.Bitmap]::FromFile($sourcePath)
    try {
        $width = [Math]::Min($source.Width, [int]($source.Height * $ratio))
        $height = [Math]::Min($source.Height, [int]($source.Width / $ratio))
        $rectangle = [Drawing.Rectangle]::new(
            [int](($source.Width - $width) / 2),
            [int](($source.Height - $height) / 2),
            $width,
            $height)
        $crop = $source.Clone($rectangle, $source.PixelFormat)
    }
    finally { $source.Dispose() }
    try { $crop.Save($targetPath, [Drawing.Imaging.ImageFormat]::Jpeg) }
    finally { $crop.Dispose() }
}

for ($index = 1; $index -le 14; $index++) {
    $poster = if ($index -le 12) { "poster-$index.jpg" } elseif ($index -eq 13) { 'bookworm.jpg' } else { 'rabbit.jpg' }
    Export-AspectCrop (Join-Path $destination $poster) (Join-Path $destination "thumbnail-$index.jpg") (5.0 / 3)
}
foreach ($poster in @('bookworm.jpg', 'rabbit.jpg')) {
    $posterPath = Join-Path $destination $poster
    Export-AspectCrop $posterPath $posterPath (3.0 / 4)
}

Copy-Item (Join-Path $repo 'examples/Media/MikoApp.Media/Assets/miko-local.mp4') (Join-Path $destination 'sample.mp4')
