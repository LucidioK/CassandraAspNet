param(
    [parameter(Mandatory=$True, Position=0)][string]$FolderPathToAdd
)

function IsRunningAsAdministrator()
{
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent());
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);
}


function PathContainsDirectory([string]$folderPath)
{
    [string]$currentPath = $env:path;
    $items = $currentPath.Split(';');
    $contains = $false;
    foreach ($item in $items)
    {
        $contains = $contains -or ($item -ieq $folderPath);
    }
    return $contains;
}

$FolderPathToAdd = Resolve-Path $FolderPathToAdd;

if (!(Test-Path $FolderPathToAdd))
{
    throw "$FolderPathToAdd not found.";
}

if (!(IsRunningAsAdministrator))
{
    throw "You must execute this script as administrator.";
}

$FolderPathToAdd = $FolderPathToAdd.Trim('\').Trim(' ').Trim(';').Trim('/');


Write-Host "Adding $FolderPathToAdd to the path, if needed..." -ForegroundColor Green;

[string]$currentPath = $env:path;
$alreadyInPath = PathContainsDirectory $FolderPathToAdd;
if ($alreadyInPath)
{
    Write-Host "$FolderPathToAdd already in path." -ForegroundColor Green;
}
else
{
    Write-Host "Adding $FolderPathToAdd to path..." -ForegroundColor Green;
    [string]$newPath="$currentPath;$FolderPathToAdd";
    Set-ItemProperty -Path ‘Registry::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Environment’ -Name PATH -Value $newPath;
    $env:path = $newPath;
}

Write-Host "Done." -ForegroundColor Green;