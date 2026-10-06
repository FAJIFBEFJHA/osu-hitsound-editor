$ErrorActionPreference = "Stop"

function Invoke-NativeCommandCaptured
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference

    try
    {
        # Native commands such as Git may legitimately write warnings to stderr.
        # Do not allow PowerShell to convert those warnings into terminating errors.
        $ErrorActionPreference = "Continue"

        $output = @(
            & $Command @Arguments 2>&1 |
                ForEach-Object { $_.ToString() }
        )

        $exitCode = $LASTEXITCODE
    }
    finally
    {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [PSCustomObject]@{
        Output = $output
        ExitCode = $exitCode
    }
}

function Add-Section
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    "" | Add-Content -Path $Path -Encoding utf8
    "============================================================" |
        Add-Content -Path $Path -Encoding utf8
    $Title |
        Add-Content -Path $Path -Encoding utf8
    "============================================================" |
        Add-Content -Path $Path -Encoding utf8
}

$repoResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("rev-parse", "--show-toplevel")

if ($repoResult.ExitCode -ne 0)
{
    throw "The current directory is not inside a Git repository."
}

$repoRoot = $repoResult.Output[0].Trim()

Set-Location $repoRoot

$repoName = Split-Path $repoRoot -Leaf
$parentDirectory = Split-Path $repoRoot -Parent

$generatedAt = Get-Date
$dateFolder = $generatedAt.ToString("yyyy-MM-dd")
$timestamp = $generatedAt.ToString("yyyy-MM-dd_HH-mm-ss")

$reviewRoot = Join-Path `
    $parentDirectory `
    "$repoName-session-reviews"

$reviewDirectory = Join-Path `
    $reviewRoot `
    $dateFolder

New-Item `
    -ItemType Directory `
    -Path $reviewDirectory `
    -Force |
    Out-Null

$outputPath = Join-Path `
    $reviewDirectory `
    "$repoName-session-review_$timestamp.txt"

@(
    "OSU HITSOUND EDITOR - SESSION REVIEW"
    "Generated: $($generatedAt.ToString('yyyy-MM-dd HH:mm:ss'))"
    "Repository: $repoRoot"
) | Set-Content -Path $outputPath -Encoding utf8

Add-Section -Title "DOTNET TEST" -Path $outputPath

$testResult = Invoke-NativeCommandCaptured `
    -Command "dotnet" `
    -Arguments @("test")

if ($testResult.Output.Count -gt 0)
{
    $testResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}

"" | Add-Content -Path $outputPath -Encoding utf8
"dotnet test exit code: $($testResult.ExitCode)" |
    Add-Content -Path $outputPath -Encoding utf8

Add-Section -Title "GIT STATUS" -Path $outputPath

$statusResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("status", "--short")

if ($statusResult.Output.Count -gt 0)
{
    $statusResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "(clean)" |
        Add-Content -Path $outputPath -Encoding utf8
}

Add-Section -Title "DIFF STAT" -Path $outputPath

$diffStatResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("diff", "--stat")

if ($diffStatResult.Output.Count -gt 0)
{
    $diffStatResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "(no tracked unstaged changes)" |
        Add-Content -Path $outputPath -Encoding utf8
}

Add-Section -Title "DIFF CHECK" -Path $outputPath

$diffCheckResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("diff", "--check")

if ($diffCheckResult.Output.Count -gt 0)
{
    $diffCheckResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "(no issues)" |
        Add-Content -Path $outputPath -Encoding utf8
}

"" | Add-Content -Path $outputPath -Encoding utf8
"git diff --check exit code: $($diffCheckResult.ExitCode)" |
    Add-Content -Path $outputPath -Encoding utf8

Add-Section `
    -Title "UNSTAGED TRACKED DIFF" `
    -Path $outputPath

$unstagedResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @(
        "diff",
        "--no-ext-diff",
        "--text"
    )

if ($unstagedResult.Output.Count -gt 0)
{
    $unstagedResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "(none)" |
        Add-Content -Path $outputPath -Encoding utf8
}

Add-Section `
    -Title "STAGED DIFF" `
    -Path $outputPath

$stagedResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @(
        "diff",
        "--cached",
        "--no-ext-diff",
        "--text"
    )

if ($stagedResult.Output.Count -gt 0)
{
    $stagedResult.Output |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "(none)" |
        Add-Content -Path $outputPath -Encoding utf8
}

Add-Section `
    -Title "UNTRACKED FILES" `
    -Path $outputPath

$untrackedResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @(
        "ls-files",
        "--others",
        "--exclude-standard"
    )

$untrackedFiles = $untrackedResult.Output

$textExtensions = @(
    ".cs",
    ".csproj",
    ".md",
    ".txt",
    ".json",
    ".xml",
    ".props",
    ".targets",
    ".ps1",
    ".yml",
    ".yaml",
    ".sln",
    ".slnx"
)

if ($untrackedFiles.Count -eq 0)
{
    "(none)" |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    foreach ($file in $untrackedFiles)
    {
        "" | Add-Content -Path $outputPath -Encoding utf8

        "----- $file -----" |
            Add-Content -Path $outputPath -Encoding utf8

        $fullPath = Join-Path $repoRoot $file

        $extension =
            [System.IO.Path]::GetExtension(
                $file
            ).ToLowerInvariant()

        $name =
            [System.IO.Path]::GetFileName($file)

        if (
            $extension -in $textExtensions -or
            $name -eq ".editorconfig"
        )
        {
            Get-Content `
                -LiteralPath $fullPath `
                -Raw |
                Add-Content `
                    -Path $outputPath `
                    -Encoding utf8
        }
        else
        {
            "[Binary or unsupported text file - content omitted]" |
                Add-Content `
                    -Path $outputPath `
                    -Encoding utf8
        }
    }
}

Add-Section -Title "SUMMARY" -Path $outputPath

if ($testResult.ExitCode -eq 0)
{
    "Tests: PASS" |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "Tests: FAIL" |
        Add-Content -Path $outputPath -Encoding utf8
}

if ($statusResult.Output.Count -gt 0)
{
    "Repository has uncommitted changes." |
        Add-Content -Path $outputPath -Encoding utf8
}
else
{
    "Repository is clean." |
        Add-Content -Path $outputPath -Encoding utf8
}

Write-Host ""
Write-Host "Session review created:"
Write-Host $outputPath

if ($testResult.ExitCode -ne 0)
{
    Write-Warning "Tests failed. Review the report before committing."
}

if ($diffCheckResult.ExitCode -ne 0)
{
    Write-Warning "git diff --check reported issues. Review the report before committing."
}
