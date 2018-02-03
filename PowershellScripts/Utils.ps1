#
# Utils.ps1
#

[string]$global:docker="";
function global:StartDockerContainerIfNeeded([string]$containerName)
{
    [string]$docker = global:GetDocker;

    &$docker ('container', 'start', $containerName) | Out-Null;
    if (!($?))
    {
        throw "Could not start docker container $containerName, please create it";
    }
}

function global:FindExecutableInPath([string]$executableName)
{
    $path=$null;
    $env:Path.Split(';') | foreach {
        if (($_ -ne $null) -and ($_.Length -gt 0) -and (Test-Path $_ -PathType Container))
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


function global:IsUsableList($l)
{
    if ($l -ne $null)
    {
        if ($l.GetType().BaseType.ToString() -eq 'System.Array')
        {
            return $true;
        }
    }
    return $false;
}

function global:listUnion($l1, $l2)
{
    $lr=@();
    if (global:IsUsableList($l1))
    {
        $l1 | foreach { $lr += $_ };
    }
    if (global:IsUsableList($l2))
    {
        $l2 | foreach { $lr += $_ };
    }
    return $lr;
}

function global:GetDocker()
{
    if ($global:docker.Length -eq 0)
    {
        $global:docker = FindExecutableInPathThrowIfNotFound 'docker' 'Please install docker';
    }
    return $global:docker;
}

function global:GetCassandraKeySpaceNamesFromDockerContainer([string]$CassandraDockerContainer)
{
    [string]$docker = global:GetDocker;
    $keySpaceNames = &$docker ('exec', '--privileged', '-it', $CassandraDockerContainer, 'cqlsh', '-e', 'describe keyspaces;');
    if (global:IsUsableList($keySpaceNames))
    {
        $keySpaceNames = [System.String]::Join(' ', $keySpaceNames);
    }
    $keySpaceNames = ($keySpaceNames -replace ' +',' ').Split(' ');
    return $keySpaceNames;
}