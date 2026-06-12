[CmdletBinding()]
param(
    [string]$PythonVersion = "3.12.8",
    [string]$DiagramsVersion = "0.24.4",
    [string]$GraphvizPythonVersion = "0.20.3",
    [string]$GraphvizBinDir,
    [string]$OutputRoot = "artifacts/runtime"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputRootPath = Join-Path $repoRoot $OutputRoot
$pythonRoot = Join-Path $outputRootPath "python"
$graphvizRoot = Join-Path $outputRootPath "graphviz"
$cacheRoot = Join-Path $repoRoot "artifacts/cache"

New-Item -ItemType Directory -Force -Path $cacheRoot, $pythonRoot, $graphvizRoot | Out-Null

function Resolve-GraphvizRoot {
    param(
        [string]$ConfiguredPath
    )

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $candidates += $ConfiguredPath
    }

    $candidates += "C:\Program Files\Graphviz"

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        $resolved = Resolve-Path -LiteralPath $candidate -ErrorAction SilentlyContinue
        if (-not $resolved) {
            continue
        }

        $resolvedPath = $resolved.ProviderPath
        $rootDot = Join-Path $resolvedPath "bin/dot.exe"
        if (Test-Path -LiteralPath $rootDot) {
            return $resolvedPath
        }

        $binDot = Join-Path $resolvedPath "dot.exe"
        if (Test-Path -LiteralPath $binDot) {
            return (Split-Path -Parent $resolvedPath)
        }
    }

    return $null
}

$pythonZip = Join-Path $cacheRoot "python-$PythonVersion-embed-amd64.zip"
$pythonUrl = "https://www.python.org/ftp/python/$PythonVersion/python-$PythonVersion-embed-amd64.zip"

if (-not (Test-Path -LiteralPath $pythonZip)) {
    Write-Host "Downloading Python embeddable runtime $PythonVersion"
    Invoke-WebRequest -Uri $pythonUrl -OutFile $pythonZip
}

Write-Host "Extracting Python runtime"
Remove-Item -LiteralPath $pythonRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $pythonRoot | Out-Null
Expand-Archive -LiteralPath $pythonZip -DestinationPath $pythonRoot -Force

$pth = Get-ChildItem -LiteralPath $pythonRoot -Filter "python*._pth" | Select-Object -First 1
if ($pth) {
    $content = Get-Content -LiteralPath $pth.FullName
    $content = $content | ForEach-Object {
        if ($_ -eq "#import site") { "import site" } else { $_ }
    }
    Set-Content -LiteralPath $pth.FullName -Value $content -Encoding ASCII
}

$sitePackages = Join-Path $pythonRoot "Lib/site-packages"
New-Item -ItemType Directory -Force -Path $sitePackages | Out-Null

$getPip = Join-Path $cacheRoot "get-pip.py"
if (-not (Test-Path -LiteralPath $getPip)) {
    Write-Host "Downloading get-pip.py"
    Invoke-WebRequest -Uri "https://bootstrap.pypa.io/get-pip.py" -OutFile $getPip
}

$pythonExe = Join-Path $pythonRoot "python.exe"
& $pythonExe $getPip --no-warn-script-location
& $pythonExe -m pip install --no-cache-dir --target $sitePackages "diagrams==$DiagramsVersion" "graphviz==$GraphvizPythonVersion"

$resolvedGraphviz = Resolve-GraphvizRoot -ConfiguredPath $GraphvizBinDir
if ($resolvedGraphviz) {
    Write-Host "Using Graphviz install at $resolvedGraphviz"
    Write-Host "Copying Graphviz runtime"
    Remove-Item -LiteralPath $graphvizRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $graphvizRoot | Out-Null
    $graphvizItems = Get-ChildItem -LiteralPath $resolvedGraphviz -Force
    Copy-Item -LiteralPath $graphvizItems.FullName -Destination $graphvizRoot -Recurse -Force
}
elseif ([string]::IsNullOrWhiteSpace($GraphvizBinDir)) {
    Write-Warning "Graphviz native binaries were not staged. Install Graphviz at C:\Program Files\Graphviz, or re-run with -GraphvizBinDir pointing to a Graphviz install directory that contains bin\dot.exe."
}
else {
    throw "GraphvizBinDir must point to a Graphviz root that contains bin\dot.exe, or to the Graphviz bin directory that contains dot.exe."
}

Write-Host "Runtime staged at $outputRootPath"
