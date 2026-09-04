param(
	[Parameter(Mandatory = $true)][string]$ScreenshotPath,
	[Parameter(Mandatory = $true)][string]$ApparatusPath,
	[Parameter(Mandatory = $true)][string]$ForgePath,
	[Parameter(Mandatory = $true)][string]$OutputPath
)

Add-Type -AssemblyName System.Drawing

function Remove-EdgeBackground([System.Drawing.Bitmap]$source) {
	$result = New-Object System.Drawing.Bitmap $source.Width, $source.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$visited = New-Object 'bool[,]' $source.Width, $source.Height
	$queue = [System.Collections.Generic.Queue[System.Drawing.Point]]::new()

	# Flood only the pale neutral backdrop connected to an image edge.
	for ($x = 0; $x -lt $source.Width; $x++) {
		$queue.Enqueue([System.Drawing.Point]::new($x, 0))
		$queue.Enqueue([System.Drawing.Point]::new($x, $source.Height - 1))
	}
	for ($y = 0; $y -lt $source.Height; $y++) {
		$queue.Enqueue([System.Drawing.Point]::new(0, $y))
		$queue.Enqueue([System.Drawing.Point]::new($source.Width - 1, $y))
	}

	while ($queue.Count -gt 0) {
		$point = $queue.Dequeue()
		if ($point.X -lt 0 -or $point.Y -lt 0 -or $point.X -ge $source.Width -or $point.Y -ge $source.Height -or $visited[$point.X, $point.Y]) {
			continue
		}
		$color = $source.GetPixel($point.X, $point.Y)
		$spread = [Math]::Max($color.R, [Math]::Max($color.G, $color.B)) - [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
		if ([Math]::Min($color.R, [Math]::Min($color.G, $color.B)) -lt 205 -or $spread -gt 22) {
			continue
		}
		$visited[$point.X, $point.Y] = $true
		$queue.Enqueue([System.Drawing.Point]::new($point.X - 1, $point.Y))
		$queue.Enqueue([System.Drawing.Point]::new($point.X + 1, $point.Y))
		$queue.Enqueue([System.Drawing.Point]::new($point.X, $point.Y - 1))
		$queue.Enqueue([System.Drawing.Point]::new($point.X, $point.Y + 1))
	}

	for ($y = 0; $y -lt $source.Height; $y++) {
		for ($x = 0; $x -lt $source.Width; $x++) {
			if (-not $visited[$x, $y]) {
				$result.SetPixel($x, $y, $source.GetPixel($x, $y))
			}
		}
	}
	return $result
}

function Get-VisibleBounds([System.Drawing.Bitmap]$bitmap) {
	$left = $bitmap.Width
	$top = $bitmap.Height
	$right = -1
	$bottom = -1
	for ($y = 0; $y -lt $bitmap.Height; $y++) {
		for ($x = 0; $x -lt $bitmap.Width; $x++) {
			if ($bitmap.GetPixel($x, $y).A -gt 0) {
				$left = [Math]::Min($left, $x)
				$top = [Math]::Min($top, $y)
				$right = [Math]::Max($right, $x)
				$bottom = [Math]::Max($bottom, $y)
			}
		}
	}
	return [System.Drawing.Rectangle]::FromLTRB($left, $top, $right + 1, $bottom + 1)
}

function Convert-ToGameSprite([System.Drawing.Bitmap]$source, [int]$logicalWidth, [int]$logicalHeight) {
	$bounds = Get-VisibleBounds $source
	$logical = New-Object System.Drawing.Bitmap $logicalWidth, $logicalHeight, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$graphics = [System.Drawing.Graphics]::FromImage($logical)
	try {
		$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
		$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
		$graphics.DrawImage($source, [System.Drawing.Rectangle]::new(0, 0, $logicalWidth, $logicalHeight), $bounds, [System.Drawing.GraphicsUnit]::Pixel)
	} finally {
		$graphics.Dispose()
	}

	# Coarse channel steps suppress high-resolution micro-shading.
	for ($y = 0; $y -lt $logical.Height; $y++) {
		for ($x = 0; $x -lt $logical.Width; $x++) {
			$color = $logical.GetPixel($x, $y)
			if ($color.A -lt 96) {
				$logical.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
				continue
			}
			$r = [Math]::Min(255, [Math]::Round($color.R / 32) * 32)
			$g = [Math]::Min(255, [Math]::Round($color.G / 32) * 32)
			$b = [Math]::Min(255, [Math]::Round($color.B / 32) * 32)
			$logical.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
		}
	}

	$scaled = New-Object System.Drawing.Bitmap ($logicalWidth * 2), ($logicalHeight * 2), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$graphics = [System.Drawing.Graphics]::FromImage($scaled)
	try {
		$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
		$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
		$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
		$graphics.DrawImage($logical, 0, 0, $scaled.Width, $scaled.Height)
	} finally {
		$graphics.Dispose()
		$logical.Dispose()
	}
	return $scaled
}

$screenshot = [System.Drawing.Bitmap]::FromFile($ScreenshotPath)
$apparatusSource = [System.Drawing.Bitmap]::FromFile($ApparatusPath)
$forgeSource = [System.Drawing.Bitmap]::FromFile($ForgePath)
try {
	$apparatusCutout = Remove-EdgeBackground $apparatusSource
	$forgeCutout = Remove-EdgeBackground $forgeSource
	try {
		$apparatus = Convert-ToGameSprite $apparatusCutout 32 32
		$forge = Convert-ToGameSprite $forgeCutout 40 24
		try {
			$output = New-Object System.Drawing.Bitmap $screenshot.Width, $screenshot.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
			$graphics = [System.Drawing.Graphics]::FromImage($output)
			try {
				$graphics.DrawImageUnscaled($screenshot, 0, 0)
				# Align both sprite bases to the original roof surface.
				$graphics.DrawImageUnscaled($apparatus, 132, 269)
				$graphics.DrawImageUnscaled($forge, 262, 285)
			} finally {
				$graphics.Dispose()
			}
			$output.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
			$output.Dispose()
		} finally {
			$apparatus.Dispose()
			$forge.Dispose()
		}
	} finally {
		$apparatusCutout.Dispose()
		$forgeCutout.Dispose()
	}
} finally {
	$screenshot.Dispose()
	$apparatusSource.Dispose()
	$forgeSource.Dispose()
}
