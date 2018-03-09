#
# Utils.ps1
#
Add-Type -AssemblyName System.IO.Compression.FileSystem;

[string]$global:docker="";
[string]$global:curl  ="";

function global:GetBasicAuthorizationHeaderValue([string]$userName,[string]$password)
{
    $pair = "${userName}:${password}";
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($pair);
    $base64 = [System.Convert]::ToBase64String($bytes);
    return "Basic $base64";
}

function global:httpGetWithBasicAuth([string]$uri, [string]$userName,[string]$password, [string]$desiredFormat='application/xml')
{
    $basicAuthValue = global:GetBasicAuthorizationHeaderValue $userName $password;
    $headers = @{ Authorization = $basicAuthValue; Accept=$desiredFormat };
    $iwrr=Invoke-WebRequest -UseBasicParsing -Uri $uri -Headers $headers;
    if ($iwrr.StatusCode -ge 200 -and $iwrr.StatusCode -lt 300)
    {
        return $iwrr.Content;
    }
    return $iwrr;
}

function global:httpGetWithBasicAuthAsXml([string]$uri, [string]$userName,[string]$password)
{
    $content = global:httpGetWithBasicAuth $uri $userName $password 'application/xml';
    if ($content -ne $null -and $content.GetTypeCode() -eq 'String')
    {
        [xml]$xml=$content;
        return $xml;
    }
    return $null;
}

function global:httpGetWithBasicAuthAsJson([string]$uri, [string]$userName,[string]$password)
{
    $content = global:httpGetWithBasicAuth $uri $userName $password 'application/json';
    if ($content -ne $null -and $content.GetTypeCode() -eq 'String')
    {
        $json=$content | ConvertFrom-Json;
        return $json;
    }
    return $null;
}


function global:getEndPointsFromAtomsPubAccordingToTitle([string]$uri, [string]$userName,[string]$password, [string]$title)
{
    $atomsPubTxt = global:httpGetWithBasicAuth $uri $userName $password;
    if ($atomsPubTxt -eq $null -or $atomsPubTxt.GetTypeCode() -ne 'String')
    {
        throw "Could not retrieve AtomsPub data from $uri";
    }
    [xml]$atomsPub = $atomsPubTxt;
    $entries=$atomsPub.feed.GetElementsByTagName('entry');
    $result=@();
    foreach ($entry in $entries)
    { 
        $links = $entry.GetElementsByTagName('link'); 
        $pal = $links | where { $_.title -eq $title };
        try
        {
            $result = $result + $pal.href;
        }
        catch{}
    }
    return $result;
}


function global:getModifiedFiles()
{
    $git = global:GetGit;
    $gr=&$git ('status', '--porcelain') | foreach { Resolve-Path ([string]$_).Substring(3).Replace('/','\').Trim('"') -ErrorAction Ignore }
    return $gr;
}

function global:SaveChangedFiles()
{
    global:RemoveDirectoryIfNeeded 'SavedFiles';

    global:CreateDirectoryIfNeded 'SavedFiles';
    [string]$destinationBasePath = Resolve-Path 'SavedFiles';
    $modified = global:getModifiedFiles;
    [string]$basePath = pwd;
    foreach ($path in $modified)
    {
        $withoutBasePath = $path.Path.SubString($basePath.Length+1);
        $destinationPath = Join-Path $destinationBasePath $withoutBasePath;
        if ((Get-Item $path) -is [System.IO.DirectoryInfo])
        {
            global:CreateDirectoryIfNeded $destinationPath;
            Copy-Item -Recurse -Path $path -Destination $destinationPath -Container;
        }
        else
        {
            $destinationDirectoryPath = [System.IO.Path]::GetDirectoryName($destinationPath);
            global:CreateDirectoryIfNeded $destinationDirectoryPath;
            Copy-Item -Path $path -Destination $destinationPath;
        }
    }
    $zipFile = [System.IO.Path]::GetFileNameWithoutExtension($x.Path) ;
    $zipFile = $zipFile + ((Get-Date).ToString("yyyyMMddhhmmss"));
    $zipFile = $zipFile + ".zip";
    $zipFile = join-path $env:TEMP $zipFile; 
    [System.IO.Compression.ZipFile]::CreateFromDirectory($destinationBasePath, $zipFile);
    global:RemoveDirectoryIfNeeded 'SavedFiles';
    return $zipFile;
}

function global:CreateDirectoryIfNeded($path)
{
    if (!(Test-Path $path))
    {
        md $path;
    }
}

function global:RemoveDirectoryIfNeeded($path)
{
    if (test-path $path)
    {
        Remove-Item -Path $path -Recurse -Force;
    }
}

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

function global:GetGit()
{
    if ($global:git -eq $null -or $global:git.Length -eq 0)      
    { 
        $global:git      = FindExecutableInPathThrowIfNotFound 'git' 'Please install git';
    }
    return $global:git;
}

function global:GetDocker()
{
    if ($global:docker.Length -eq 0)
    {
        $global:docker = FindExecutableInPathThrowIfNotFound 'docker' 'Please install docker';
    }
    return $global:docker;
}

function global:GetCurl()
{
    if ($global:curl -eq $null -or $global:curl.Length -eq 0)
    {
        $global:curl = FindExecutableInPathThrowIfNotFound 'curl' 'Please install curl from https://curl.haxx.se/download.html';
    }
    return $global:curl;
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

function global:Unzip([string]$zipfile, [string]$outpath)
{
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath);
}

function global:moveDirectoryToRecycleBin([string]$folderName)
{
    $folderName = Resolve-Path $folderName;
    $parentFolder = (resolve-path (join-path $folderName "..")).Path;
    $lastPathItem = [System.IO.Path]::GetFileName($folderName);
    if ($parentFolder -eq $folderName)
    {
        throw "Cannot move $folderName to recycle bin.";
    }
    $shell = new-object -comobject "Shell.Application";
    $folder = $shell.Namespace($parentFolder);
    $item = $folder.ParseName($lastPathItem);
    $item.InvokeVerb("delete");
}

function global:revertString([string]$s)
{
    [string]$r = "";
    foreach ($c in $s.ToCharArray()) { $r = ($c + $r); }
    return $r;
}

function global:revertAndLowerString([string]$s)
{
    $s = $s.ToLowerInvariant();
    [string]$r = "";
    foreach ($c in $s.ToCharArray()) { $r = ($c + $r); }
    return $r;
}

function global:getFileListWithoutRepeatedFiles($pathList)
{
    $fileList = @();
    foreach ($path in $pathList)
    {
        Get-ChildItem -Path $path | 
            foreach { $fileList = $fileList + (global:revertAndLowerString $_.FullName); } 
    }
    $fileList = $fileList | Sort-Object;
    $finalList = @();
    $previousInvName="!!!!";
    foreach ($file in $fileList)
    {
        [string]$fn = $file;
        $invName=$fn.Split('\')[0];work
        if ($previousInvName -ne $invName)
        {
            $finalList = ($finalList + (global:revertAndLowerString $fn));
        }
    }
    return $finalList;
}



