&(join-path $PSScriptRoot 'Utils.ps1');
$dirs = Get-ChildItem -Directory;
$git=global:getGit;
foreach ($dir in $dirs)
{
    pushd .
    cd $dir;
    if (Test-Path ".git")
    {
        Write-Host $dir -ForegroundColor Green;
        &$git  ('pull', 'origin', 'master');
    }
    popd
}