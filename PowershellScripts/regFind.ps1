param(
    [parameter(Mandatory=$True, Position=0)][string]$TextToFind
)

&(join-path $PSScriptRoot 'utils.ps1');

[string]$originalPSScriptRoot = $PSScriptRoot;

$global:reg = FindExecutableInPathThrowIfNotFound 'reg' 'Could not find reg.exe, is this a Windows machine?';

function regSearch([string]$KeyName, [string]$textToFind)
{
    Write-Host "Searching under $KeyName, this might take some time, hang on..." -ForegroundColor Green;
    $regList = &$global:reg ('query',$KeyName, '/t',  'REG_SZ', '/s',  '/f', "$TextToFind") | foreach { $_ -replace " +",'|' };
    $listCount = $regList.Length;
    Write-Host "Found $listCount items..." -ForegroundColor Green;
    return $regList;
}

$l1 = regSearch 'HKCU' $TextToFind;
$l2 = regSearch 'HKLM' $TextToFind;
$regList = global:listUnion $l1 $l2;
$regList;
