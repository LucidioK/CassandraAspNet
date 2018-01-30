#
# CreateToolsFolder.ps1
#
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
$csprojs = Get-ChildItem -Path '.' -Filter '*.csproj' -Recurse;

foreach ($csproj in $csprojs)
{
	[string]$toBuild=$csproj.DirectoryName;
    cd $toBuild;
    &$dotnet ('clean');
    &$dotnet ('publish',
                '--self-contained',
                '--runtime',       $runtime,
                '--configuration', 'Debug',
                '--verbosity',     'Minimal');
}

popd

$publishFolders =  Get-ChildItem -Path '.' -Filter 'publish' -Recurse -Directory;
$publishFolders |
    foreach {
        Copy-Item -Path $_ -Destination $ToolsPath -ErrorAction SilentlyContinue -WarningAction SilentlyContinue;
    };

Copy-Item -Path $originalPSScriptRoot -Destination $ToolsPath;