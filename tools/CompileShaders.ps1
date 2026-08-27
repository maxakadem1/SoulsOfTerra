[CmdletBinding()]
param(
	[switch]$Force,
	[switch]$Check,
	[string]$CompilerPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$effectsRoot = Join-Path $repoRoot 'Effects'

function Find-EffectCompiler
{
	if (![string]::IsNullOrWhiteSpace($CompilerPath))
	{
		return [IO.Path]::GetFullPath($CompilerPath)
	}

	$fromPath = Get-Command 'fxc.exe' -ErrorAction SilentlyContinue
	if ($null -ne $fromPath)
	{
		return $fromPath.Source
	}

	$kitsRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
	if (Test-Path -LiteralPath $kitsRoot)
	{
		$installed = Get-ChildItem -LiteralPath $kitsRoot -Filter 'fxc.exe' -File -Recurse |
			Where-Object { $_.DirectoryName -match '\\(x64|x86)$' } |
			Sort-Object { [version]($_.Directory.Parent.Name) } -Descending
		if ($installed.Count -gt 0)
		{
			return $installed[0].FullName
		}
	}

	throw 'fxc.exe was not found. Install the Windows SDK or pass -CompilerPath.'
}

if (!(Test-Path -LiteralPath $effectsRoot))
{
	throw "Effects directory not found: $effectsRoot"
}

$sources = @(Get-ChildItem -LiteralPath $effectsRoot -Filter '*.fx' -File -Recurse | Sort-Object FullName)
$stale = @($sources | Where-Object {
	$compiledPath = [IO.Path]::ChangeExtension($_.FullName, '.fxc')
	!(Test-Path -LiteralPath $compiledPath) -or $_.LastWriteTimeUtc -gt (Get-Item -LiteralPath $compiledPath).LastWriteTimeUtc
})

$toCompile = @(if ($Force -or $Check) { $sources } else { $stale })
if ($toCompile.Count -eq 0)
{
	Write-Host "All $($sources.Count) shaders are current."
	exit 0
}

$resolvedCompiler = Find-EffectCompiler
if (!(Test-Path -LiteralPath $resolvedCompiler))
{
	throw "Shader compiler not found at $resolvedCompiler"
}

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "SoulsOfTerra-ShaderBuild-$PID"
try
{
	[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
	$checkFailures = [Collections.Generic.List[string]]::new()
	foreach ($source in $toCompile)
	{
		$relativePath = [IO.Path]::GetRelativePath($repoRoot, $source.FullName)
		$temporaryOutput = Join-Path $temporaryRoot ([guid]::NewGuid().ToString('N') + '.fxc')
		Write-Host "Compiling $relativePath"

		& $resolvedCompiler /nologo /T fx_2_0 /Fo $temporaryOutput $source.FullName
		if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $temporaryOutput))
		{
			throw "Shader compilation failed for $relativePath."
		}

		$bytes = [IO.File]::ReadAllBytes($temporaryOutput)
		if ($bytes.Length -lt 4 -or $bytes[0] -ne 1 -or $bytes[1] -ne 9 -or $bytes[2] -ne 255 -or $bytes[3] -ne 254)
		{
			throw "Compiler produced a non-FNA Effect binary for $relativePath."
		}

		$trackedOutput = [IO.Path]::ChangeExtension($source.FullName, '.fxc')
		if ($Check)
		{
			if (!(Test-Path -LiteralPath $trackedOutput) -or (Get-FileHash -LiteralPath $temporaryOutput -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $trackedOutput -Algorithm SHA256).Hash)
			{
				$checkFailures.Add($relativePath)
			}
		}
		else
		{
			# Replace the tracked asset only after compilation and validation succeed.
			Copy-Item -LiteralPath $temporaryOutput -Destination $trackedOutput -Force
		}
	}

	if ($Check -and $checkFailures.Count -gt 0)
	{
		throw "Compiled shaders do not match their sources:`n$($checkFailures -join "`n")"
	}

	if ($Check)
	{
		Write-Host "Verified $($toCompile.Count) shader source/bytecode pair(s)."
	}
	else
	{
		Write-Host "Compiled $($toCompile.Count) shader(s) with $resolvedCompiler"
	}
}
finally
{
	$resolvedTemp = [IO.Path]::GetFullPath($temporaryRoot)
	$tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
	if ((Test-Path -LiteralPath $resolvedTemp) -and $resolvedTemp.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and ([IO.Path]::GetFileName($resolvedTemp) -like 'SoulsOfTerra-ShaderBuild-*'))
	{
		Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
	}
}
