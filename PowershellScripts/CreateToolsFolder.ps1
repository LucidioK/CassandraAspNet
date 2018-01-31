param(
    [parameter(Mandatory=$False, Position=0)][string]$DirectoryToBuild='.'
)

&(join-path $PSScriptRoot 'utils.ps1');

[string]$originalPSScriptRoot = $PSScriptRoot;

[string]$solutionFile = global:findFileUpAndUp("CreateCassandraDBFromCode.sln");

if ($solutionFile -eq $null -or !(Test-Path $solutionFile))
{
	throw "Run CreateToolsFolder.ps1 from the folder where CreateCassandraDBFromCode.sln is.";
}

[string]$baseFolder = [System.IO.Path]::GetDirectoryName($solutionFile);
cd $baseFolder;
[string]$ToolsPath = Resolve-Path 'Tools';
pushd .
$dotnet = FindExecutableInPathThrowIfNotFound 'dotnet' 'Please install dotnet core from https://www.microsoft.com/net/download/windows#core';


if (!(Test-Path $ToolsPath -PathType Container))
{
	New-Item -ItemType Directory -Name $ToolsPath;
}

$runtime = global:GetRuntime;
$csprojs = Get-ChildItem -Path $DirectoryToBuild -Filter '*.csproj' -Recurse;

foreach ($csproj in $csprojs)
{
	[string]$toBuild=$csproj.DirectoryName;
    [string]$fileName=$csproj.Name;
    cd $toBuild;
    Write-Host "Building $fileName..." -ForegroundColor Green;
    &$dotnet ('clean');

    &$dotnet ('publish',
                '--self-contained',
                '--runtime',       $runtime,
                '--configuration', 'Debug',
                '--verbosity',     'Minimal');
}

popd

$publishFolders =  Get-ChildItem -Path $DirectoryToBuild -Filter 'publish' -Recurse -Directory;
$publishFolders =  $publishFolders | where {(!($_.FullName.ToLowerInvariant().Contains('\x')))};
$publishFolders |
    foreach {
        $src=($_.FullName + "\*.*");
        Write-Host "Copying $src to the $ToolsPath folder..." -ForegroundColor Green;
        copy-item -Path  $src -Destination $ToolsPath -Container -ErrorAction SilentlyContinue -WarningAction SilentlyContinue;
    };

Copy-Item -Path ($originalPSScriptRoot + "\*.*") -Destination $ToolsPath;

Write-Host "Done." -ForegroundColor Green;