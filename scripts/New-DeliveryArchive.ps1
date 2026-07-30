[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version = '1.0.0',

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..')
)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'release'
}

$resolvedOutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$archiveName = "DashboardProject-v$Version-source.zip"
$archivePath = Join-Path $resolvedOutputDirectory $archiveName
$checksumPath = Join-Path $resolvedOutputDirectory 'SHA256SUMS.txt'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$stagingPath = Join-Path(
    $temporaryRoot,
    "DashboardProject-release-$([Guid]::NewGuid().ToString('N'))"
)

$expectedSeedHash =
    'A8165F4C37F80430408F9290189E158CBD17E6B817884283A2EC0EEA19052EB8'

$forbiddenEntryPattern =
    '(^|/)(\.git(?:/|$)|\.env$|database/dashboard\.db$|' +
    'database/SQLiteQuery[^/]*\.sql$|backups/.*\.(?:db|sqlite|zip|bak|tar|gz)$|' +
    'node_modules(?:/|$)|bin(?:/|$)|obj(?:/|$)|dist(?:/|$))'

try {
    $insideWorkTree = & git -C $repositoryRoot rev-parse --is-inside-work-tree
    if ($LASTEXITCODE -ne 0 -or $insideWorkTree -ne 'true') {
        throw "Repository root could not be verified: $repositoryRoot"
    }

    $sourceFiles = @(
        & git -C $repositoryRoot `
            -c core.quotepath=false `
            ls-files --cached --others --exclude-standard
    )

    if ($LASTEXITCODE -ne 0 -or $sourceFiles.Count -eq 0) {
        throw 'No delivery source files were found.'
    }

    New-Item -ItemType Directory -Path $stagingPath | Out-Null

    foreach ($relativePath in $sourceFiles) {
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            continue
        }

        $normalizedRelativePath = $relativePath.Replace('\', '/')
        if ($normalizedRelativePath -match $forbiddenEntryPattern) {
            throw "Forbidden source path selected for delivery: $normalizedRelativePath"
        }

        $sourcePath = [IO.Path]::GetFullPath(
            (Join-Path $repositoryRoot $relativePath)
        )

        if (-not $sourcePath.StartsWith(
                $repositoryRoot + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "Source path escapes the repository: $relativePath"
        }

        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            # A tracked file can be intentionally deleted in the working tree.
            continue
        }

        $destinationPath = Join-Path $stagingPath $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath
        New-Item -ItemType Directory -Force -Path $destinationDirectory |
            Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath
    }

    $seedPath = Join-Path $stagingPath 'database\seed\dashboard.seed.db'
    if (-not (Test-Path -LiteralPath $seedPath -PathType Leaf)) {
        throw 'The sanitized SQLite seed is missing from the delivery snapshot.'
    }

    $actualSeedHash = (Get-FileHash $seedPath -Algorithm SHA256).Hash
    if ($actualSeedHash -ne $expectedSeedHash) {
        throw "Sanitized seed checksum mismatch: $actualSeedHash"
    }

    New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory |
        Out-Null

    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $stagingPath,
        $archivePath,
        [IO.Compression.CompressionLevel]::Optimal,
        $false
    )

    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entries = @($archive.Entries | ForEach-Object {
            $_.FullName.Replace('\', '/')
        })

        $forbiddenEntries = @(
            $entries | Where-Object { $_ -match $forbiddenEntryPattern }
        )

        if ($forbiddenEntries.Count -gt 0) {
            throw "Forbidden archive entries found: $($forbiddenEntries -join ', ')"
        }

        if ($entries -notcontains 'database/seed/dashboard.seed.db') {
            throw 'The sanitized SQLite seed is missing from the archive.'
        }
    }
    finally {
        $archive.Dispose()
    }

    $archiveHash = (Get-FileHash $archivePath -Algorithm SHA256).Hash
    Set-Content `
        -LiteralPath $checksumPath `
        -Encoding ascii `
        -NoNewline `
        -Value "$archiveHash *$archiveName`n"

    [PSCustomObject]@{
        Archive = $archivePath
        Sha256 = $archiveHash
        Files = $entries.Count
        ChecksumFile = $checksumPath
    }
}
finally {
    $resolvedStagingPath = [IO.Path]::GetFullPath($stagingPath)
    $safeStagingName =
        (Split-Path -Leaf $resolvedStagingPath) -like
        'DashboardProject-release-*'

    if ((Test-Path -LiteralPath $resolvedStagingPath) -and
        $safeStagingName -and
        $resolvedStagingPath.StartsWith(
            $temporaryRoot,
            [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedStagingPath -Recurse -Force
    }
}
