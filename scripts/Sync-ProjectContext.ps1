$ErrorActionPreference = "Stop"

$repoResult = @(
    & git rev-parse --show-toplevel 2>&1 |
        ForEach-Object { $_.ToString() }
)

if ($LASTEXITCODE -ne 0 -or $repoResult.Count -eq 0)
{
    throw "The current directory is not inside a Git repository."
}

$repoRoot = $repoResult[0].Trim()
$sourceDirectory = Join-Path $repoRoot "docs"

$destinationDirectory = $env:OSU_HITSOUND_CONTEXT_MIRROR

if ([string]::IsNullOrWhiteSpace($destinationDirectory))
{
    throw "OSU_HITSOUND_CONTEXT_MIRROR is not configured. Set it to the local continuity-mirror directory before running this script."
}

$destinationDirectory =
    $destinationDirectory.Trim().Trim('"').Trim("'")

$destinationDirectory =
    [Environment]::ExpandEnvironmentVariables(
        $destinationDirectory)

$destinationDirectory =
    [System.IO.Path]::GetFullPath(
        $destinationDirectory)

$repoRoot =
    [System.IO.Path]::GetFullPath(
        $repoRoot).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)

$destinationDirectory =
    [System.IO.Path]::GetFullPath(
        $destinationDirectory).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)

$repoPrefix =
    $repoRoot +
    [System.IO.Path]::DirectorySeparatorChar

$destinationIsInsideRepository =
    $destinationDirectory.Equals(
        $repoRoot,
        [System.StringComparison]::OrdinalIgnoreCase) -or
    $destinationDirectory.StartsWith(
        $repoPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)

if ($destinationIsInsideRepository)
{
    throw "OSU_HITSOUND_CONTEXT_MIRROR must point outside the Git repository."
}

if (-not (Test-Path -LiteralPath $sourceDirectory))
{
    throw "The repository docs directory does not exist."
}

$files = @(
    "PROJECT_CONTEXT.md",
    "ROADMAP.md",
    "DECISIONS.md",
    "WORKFLOW.md",
    "AI_WORKFLOW.md"
)

foreach ($file in $files)
{
    $sourceFile = Join-Path $sourceDirectory $file

    if (-not (Test-Path -LiteralPath $sourceFile))
    {
        throw "Required project context file not found: $file"
    }
}

if (-not (Test-Path -LiteralPath $destinationDirectory))
{
    New-Item `
        -ItemType Directory `
        -Path $destinationDirectory `
        -Force |
        Out-Null
}

foreach ($file in $files)
{
    $sourceFile = Join-Path $sourceDirectory $file
    $destinationFile = Join-Path $destinationDirectory $file

    Copy-Item `
        -LiteralPath $sourceFile `
        -Destination $destinationFile `
        -Force

    $sourceHash =
        (Get-FileHash `
            -LiteralPath $sourceFile `
            -Algorithm SHA256).Hash

    $destinationHash =
        (Get-FileHash `
            -LiteralPath $destinationFile `
            -Algorithm SHA256).Hash

    if ($sourceHash -ne $destinationHash)
    {
        throw "Synchronization verification failed for: $file"
    }

    Write-Host "Synchronized: $file"
}

Write-Host ""
Write-Host "Project context synchronized successfully."
