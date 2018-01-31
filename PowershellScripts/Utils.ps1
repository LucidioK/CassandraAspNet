#
# Utils.ps1
#

function global:FindExecutableInPath([string]$executableName)
{
    $path=$null;
    $env:Path.Split(';') | foreach {
        if (Test-Path $_ -PathType Container)
        {
            [string]$fn = Join-Path $_ $executableName;
            [string]$fnExe = Join-Path $_ ($executableName + ".exe");
            [string]$fnCmd = Join-Path $_ ($executableName + ".cmd");
            [string]$fnBat = Join-Path $_ ($executableName + ".bat");
            if ($path -eq $null -and (Test-Path $fn))
            {
                $path=$fn;
            }
			if ($path -eq $null -and (Test-Path $fnExe))
            {
                $path=$fnExe;
            }   
            if ($path -eq $null -and (Test-Path $fnExe))
            {
                $path=$fnCmd;
            }            
			if ($path -eq $null -and (Test-Path $fnExe))
            {
                $path=$fnBat;
            }        
		}
    }
    return $path;
}

function global:FindExecutableInPathThrowIfNotFound([string]$executableName, [string]$messageInCaseOfNotFound)
{
    $exec=global:FindExecutableInPath $executableName;
    if ($exec -eq $null)
    {
        throw $messageInCaseOfNotFound;
    }
    return $exec;
}

function global:findFileUpAndUp([string]$filePattern)
{
    $path = $PSScriptRoot;
    while ($path -ne $null -and !(Test-Path (Join-Path $path $filePattern)))
    {
		try
		{
			$path = resolve-path(join-path $path '..');
		}
		catch
		{
			$path = $null;
		}
    }
	if ($path -ne $null)
	{
		return (Join-Path $path $filePattern);
	}
    return $null;
}


function global:IsWindows()
{
    return [System.Boolean](Get-CimInstance -ClassName Win32_OperatingSystem -ErrorAction SilentlyContinue);
}

function global:CreateDirectoriesIfNotExist($pathList)
{
    foreach ($path in $pathList)
    {
        if (!(Test-Path $path))
        {
            [System.IO.Directory]::CreateDirectory($path);
        }
    }
}

function global:GetRuntime()
{
    if (global:IsWindows)
    {
        return "win10-x64";
    }
    return "linux-x64";
}

function global:AddToPathIfNeeded([string]$folderToAdd)
{
    $found = $false;
    $folderToAdd = $folderToAdd.ToLowerInvariant();
    $env:Path.Split(';') | foreach {
        [string]$folderToTest = $_.ToLowerInvariant();
        if ($folderToTest -eq $folderToAdd)
        {
            $found = $true;
        }   
    }
    if (!($found))
    {
        $env:Path = ($env:Path + ';' + $folderToAdd);
    }
}

function global:GetClassesFromSwaggerJson([string]$swaggerFile)
{
    $json = Get-Content $swaggerFile | Out-String | ConvertFrom-Json;
    $classList =  $json.definitions | Get-Member -MemberType NoteProperty | select { $_.Name };
    [string]$classes = "";
    foreach ($class in $classList)
    {
        if ($classes.Length > 0)
        {
            $classes = ($classes + ',');
        }
        $classes = $classes + $class;
    }
    return $classes;
}

function global:dotnetPublish()
{
    [string]$runtime = global:GetRuntime;
    [string]$dotnet  = global:FindExecutableInPathThrowIfNotFound 'dotnet' 'Please install dotnet core from https://www.microsoft.com/net/download/windows#core';

    &$dotnet ('clean');
    &$dotnet ('publish',
                '--self-contained',
                '--runtime',       $runtime,
                '--configuration', 'Debug',
                '--verbosity',     'Minimal');
}

