[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "artifacts/publish/iis-api"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputRoot = Join-Path $repoRoot $OutputPath
$cliOutput = Join-Path $outputRoot "cli"

Push-Location $repoRoot
try {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force -ErrorAction SilentlyContinue

    dotnet publish .\src\MermaidToDiagrams.API\MermaidToDiagrams.API.csproj `
        -c $Configuration `
        -f net10.0 `
        -o $outputRoot

    dotnet publish .\src\MermaidToDiagrams.CLI\MermaidToDiagrams.CLI.csproj `
        -c $Configuration `
        -f net10.0 `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -o $cliOutput

    if (Test-Path -LiteralPath ".\artifacts\runtime") {
        Copy-Item -LiteralPath ".\artifacts\runtime" -Destination (Join-Path $cliOutput "runtime") -Recurse -Force
    }
    else {
        Write-Warning "Private Python/Graphviz runtime was not found under artifacts\runtime. API validation will work, but conversion requires staged runtime or Python/Graphviz on PATH."
    }

    Write-Host "IIS API payload staged at $outputRoot"
    Write-Host "Configure IIS application pool identity with execute permission for $cliOutput\m2d.exe"
}
finally {
    Pop-Location
}
