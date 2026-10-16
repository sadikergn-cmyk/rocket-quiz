# Packs the local WebGL build for the Nadide host: rq-build.json + Build/ -> rocketquiz-webgl-<version>.zip,
# then prints the GitHub Release command (it does not run it).
# Build first: Tools > Build RocketQuiz WebGL, or
#   unity command eval 'return WebGLBuilder.Build();' --project-path Game
# Usage (Windows PowerShell 5.1 or PowerShell 7):
#   powershell -ExecutionPolicy Bypass -File tools\package_webgl.ps1 [-BuildPath Game\Builds\WebGL] [-OutDir Game\Builds]
param(
    [string]$BuildPath = "",
    [string]$OutDir = "",
    [string]$Repo = "sadikergn-cmyk/rocket-quiz"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if ($BuildPath -eq "") { $BuildPath = Join-Path $root "Game\Builds\WebGL" }
if ($OutDir -eq "") { $OutDir = Join-Path $root "Game\Builds" }

$BuildPath = (Resolve-Path $BuildPath).Path
$manifestPath = Join-Path $BuildPath "rq-build.json"
$buildDir = Join-Path $BuildPath "Build"
if (-not (Test-Path $manifestPath)) { throw "rq-build.json yok: $manifestPath (once WebGLBuilder.Build ile derle)" }
if (-not (Test-Path $buildDir)) { throw "Build klasoru yok: $buildDir" }

$manifest = Get-Content -Raw -Encoding UTF8 $manifestPath | ConvertFrom-Json
foreach ($key in @("version", "loaderUrl", "dataUrl", "frameworkUrl", "codeUrl")) {
    if (-not $manifest.$key) { throw "rq-build.json alani bos: $key" }
}
foreach ($key in @("loaderUrl", "dataUrl", "frameworkUrl", "codeUrl")) {
    $file = Join-Path $BuildPath ($manifest.$key -replace "/", "\")
    if (-not (Test-Path $file)) { throw "rq-build.json $key dosyasi yok: $file" }
}

$version = [string]$manifest.version
# Same rule as Nadide (apps/web/src/ui/unity/flag.ts): the tag becomes part of the /oyun/<version>/ path.
if ($version -cnotmatch '^[0-9A-Za-z][0-9A-Za-z._-]{0,40}$' -or $version.Contains("..")) { throw "Surum etiketi gecersiz: $version (harf/rakamla baslar; harf, rakam, . _ -; en cok 41 karakter; '..' yok)" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$zip = Join-Path $OutDir "rocketquiz-webgl-$version.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }

# System.IO.Compression instead of Compress-Archive: entry names must use "/" (the zip is unpacked on Linux).
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $manifestPath, "rq-build.json", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    foreach ($file in Get-ChildItem -Path $buildDir -Recurse -File) {
        $entry = "Build/" + ($file.FullName.Substring($buildDir.Length + 1) -replace "\\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

$sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Paket hazir: $zip ($sizeMb MB, surum $version)"
Write-Host ""
Write-Host "GitHub Release icin (elle calistir):"
Write-Host "  gh release create $version `"$zip`" --repo $Repo --title `"RocketQuiz WebGL $version`" --notes `"Nadide icin WebGL derlemesi (rq-build.json + Build/).`""
Write-Host ""
Write-Host "Sonra Nadide tarafinda UNITY_BUILD_VERSION=$version"
