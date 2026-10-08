$ErrorActionPreference = "Stop"

function Invoke-NativeCommandCaptured {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference

    try {
        # Native commands such as Git may legitimately write warnings to stderr.
        # Do not allow PowerShell to convert those warnings into terminating errors.
        $ErrorActionPreference = "Continue"

        $output = @(
            & $Command @Arguments 2>&1 |
            ForEach-Object { $_.ToString() }
        )

        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    return [PSCustomObject]@{
        Output   = $output
        ExitCode = $exitCode
    }
}

function Add-Section {
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

function Convert-ToPortableOutput {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [AllowEmptyString()]
        [string[]]$Lines,

        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $pathVariants = @(
        $RepositoryRoot,
        ($RepositoryRoot -replace "/", "\"),
        ($RepositoryRoot -replace "\\", "/")
    ) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique

    return @(
        foreach ($line in $Lines) {
            $portableLine = $line

            foreach ($pathVariant in $pathVariants) {
                $portableLine = [System.Text.RegularExpressions.Regex]::Replace(
                    $portableLine,
                    [System.Text.RegularExpressions.Regex]::Escape($pathVariant),
                    "<repo-root>",
                    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            }

            $portableLine
        }
    )
}

$repoResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("rev-parse", "--show-toplevel")

if ($repoResult.ExitCode -ne 0) {
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

$fileName = "$repoName-session-review_$timestamp.txt"

$outputPath = Join-Path `
    $reviewDirectory `
    $fileName

@(
    "OSU HITSOUND EDITOR - SESSION REVIEW"
    "Generated: $($generatedAt.ToString('yyyy-MM-dd HH:mm:ss'))"
    "Repository: $repoName"
) | Set-Content -Path $outputPath -Encoding utf8

Add-Section -Title "DOTNET TEST" -Path $outputPath

$testResult = Invoke-NativeCommandCaptured `
    -Command "dotnet" `
    -Arguments @("test")

$portableTestOutput = Convert-ToPortableOutput `
    -Lines $testResult.Output `
    -RepositoryRoot $repoRoot

if ($portableTestOutput.Count -gt 0) {
    $portableTestOutput |
    Add-Content -Path $outputPath -Encoding utf8
}

"" | Add-Content -Path $outputPath -Encoding utf8
"dotnet test exit code: $($testResult.ExitCode)" |
Add-Content -Path $outputPath -Encoding utf8

Add-Section -Title "GIT STATUS" -Path $outputPath

$statusResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("status", "--short")

$portableStatusOutput = Convert-ToPortableOutput `
    -Lines $statusResult.Output `
    -RepositoryRoot $repoRoot

if ($portableStatusOutput.Count -gt 0) {
    $portableStatusOutput |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
    "(clean)" |
    Add-Content -Path $outputPath -Encoding utf8
}

Add-Section -Title "DIFF STAT" -Path $outputPath

$diffStatResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("diff", "--stat")

$portableDiffStatOutput = Convert-ToPortableOutput `
    -Lines $diffStatResult.Output `
    -RepositoryRoot $repoRoot

if ($portableDiffStatOutput.Count -gt 0) {
    $portableDiffStatOutput |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
    "(no tracked unstaged changes)" |
    Add-Content -Path $outputPath -Encoding utf8
}

Add-Section -Title "DIFF CHECK" -Path $outputPath

$diffCheckResult = Invoke-NativeCommandCaptured `
    -Command "git" `
    -Arguments @("diff", "--check")

$portableDiffCheckOutput = Convert-ToPortableOutput `
    -Lines $diffCheckResult.Output `
    -RepositoryRoot $repoRoot

if ($portableDiffCheckOutput.Count -gt 0) {
    $portableDiffCheckOutput |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
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

$portableUnstagedOutput = Convert-ToPortableOutput `
    -Lines $unstagedResult.Output `
    -RepositoryRoot $repoRoot

if ($portableUnstagedOutput.Count -gt 0) {
    $portableUnstagedOutput |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
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

$portableStagedOutput = Convert-ToPortableOutput `
    -Lines $stagedResult.Output `
    -RepositoryRoot $repoRoot

if ($portableStagedOutput.Count -gt 0) {
    $portableStagedOutput |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
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

if ($untrackedFiles.Count -eq 0) {
    "(none)" |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
    foreach ($file in $untrackedFiles) {
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
        ) {
            $rawContent = Get-Content `
                -LiteralPath $fullPath `
                -Raw

            $portableContent = Convert-ToPortableOutput `
                -Lines @($rawContent) `
                -RepositoryRoot $repoRoot

            $portableContent |
            Add-Content `
                -Path $outputPath `
                -Encoding utf8
        }
        else {
            "[Binary or unsupported text file - content omitted]" |
            Add-Content `
                -Path $outputPath `
                -Encoding utf8
        }
    }
}

Add-Section -Title "SUMMARY" -Path $outputPath

if ($testResult.ExitCode -eq 0) {
    "Tests: PASS" |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
    "Tests: FAIL" |
    Add-Content -Path $outputPath -Encoding utf8
}

if ($statusResult.Output.Count -gt 0) {
    "Repository has uncommitted changes." |
    Add-Content -Path $outputPath -Encoding utf8
}
else {
    "Repository is clean." |
    Add-Content -Path $outputPath -Encoding utf8
}

$relativeOutputPath = Join-Path `
    ".." `
(Join-Path `
        "$repoName-session-reviews" `
    (Join-Path $dateFolder $fileName))

Write-Host ""
Write-Host "Session review created:"
Write-Host $relativeOutputPath

if ($testResult.ExitCode -ne 0) {
    Write-Warning "Tests failed. Review the report before committing."
}

if ($diffCheckResult.ExitCode -ne 0) {
    Write-Warning "git diff --check reported issues. Review the report before committing."
}
