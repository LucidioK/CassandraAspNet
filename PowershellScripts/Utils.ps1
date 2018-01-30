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
			Write-Host "Checking $fn...";
            if ($path -eq $null -and (Test-Path $fn))
            {
                $path=$fn;
            }
			Write-Host "Checking $fnExe...";
			if ($fnExe.ToLowerInvariant() -eq 'c:\program files\dotnet\dotnet.exe')
			{
				Write-Host "Checking $fnExe...";
			}
			if ($path -eq $null -and (Test-Path $fnExe))
            {
                $path=$fnExe;
            }   
			Write-Host "Checking $fnExe...";
            if ($path -eq $null -and (Test-Path $fnExe))
            {
                $path=$fnCmd;
            }            
			Write-Host "Checking $fnExe...";
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

function global:GetRuntime()
{
    if (global:IsWindows)
    {
        return "win10-x64";
    }
    return "linux-x64";
}
