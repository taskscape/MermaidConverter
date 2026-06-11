[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$InnoCompiler = "ISCC.exe"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

Push-Location $repoRoot
try {
    dotnet publish .\src\MermaidToDiagrams.CLI\MermaidToDiagrams.CLI.csproj `
        -c $Configuration `
        -f net10.0 `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -o .\artifacts\publish\win-x64

    dotnet publish .\src\MermaidToDiagrams.GUI\MermaidToDiagrams.GUI.csproj `
        -c $Configuration `
        -f net10.0-windows `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -o .\artifacts\publish\win-x64

    if (-not (Test-Path -LiteralPath ".\artifacts\runtime\python\python.exe")) {
        throw "Python runtime is missing. Run tools\build-python-runtime.ps1 before packaging."
    }

    if (-not (Test-Path -LiteralPath ".\artifacts\runtime\graphviz\bin\dot.exe")) {
        throw "Graphviz runtime is missing. Run tools\build-python-runtime.ps1 -GraphvizBinDir <GraphvizRoot> before packaging."
    }

    & $InnoCompiler .\installer\MermaidToDiagrams.iss
}
finally {
    Pop-Location
}
