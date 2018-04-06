param(   
   [parameter(Mandatory=$False, Position=8)][switch]$AddToPath = $false
)

$dnt='C:\Program Files\dotnet\dotnet.exe';

if (!(Test-Path $dnt))
{
    throw "dotnet core not installed on this computer, could not find it at $dnt. You can download it from https://www.microsoft.com/net/download"
}

Write-Host "Running dotnet publish." -ForegroundColor Green;

$dntExec = &$dnt ('publish',
        '--self-contained',
        '--runtime',       'win10-x64',
        '--configuration', 'Debug',
        '--verbosity',     'Minimal');

Write-Host "Done publishing." -ForegroundColor Green;
if ($AddToPath)
{
    # dotnet publish renders output similar to this:
    # Restoring packages for C:\temp\tools\cqlExec\cqlExec.csproj...
    # Generating MSBuild file C:\temp\tools\cqlExec\obj\cqlExec.csproj.nuget.g.props.
    # Generating MSBuild file C:\temp\tools\cqlExec\obj\cqlExec.csproj.nuget.g.targets.
    # Restore completed in 3.74 sec for C:\temp\tools\cqlExec\cqlExec.csproj.
    # cqlExec -> C:\temp\tools\cqlExec\bin\Debug\netcoreapp2.0\win10-x64\cqlExec.dll
    # cqlExec -> C:\temp\tools\cqlExec\bin\Debug\netcoreapp2.0\win10-x64\publish\
    $publishedFolder = $dntExec[$dntExec.Count - 1].Split('>')[1].Trim();
    $jwt=&(join-path $PSScriptRoot 'AddToPathIfNeeded.ps1') -FolderPathToAdd $publishedFolder
}

