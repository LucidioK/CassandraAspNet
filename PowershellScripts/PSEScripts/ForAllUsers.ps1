
$contractAccountId=400000441685
[string]$uri="$(GetConfig.ps1 'mcfServer')/sap/opu/odata/sap/ZERP_UTILITIES_UMC_PSE_SRV/ContractAccounts"
$userList = @();
Write-Host "Retrieving users from tabl selfservice_auth.user_auth... " -ForegroundColor green -NoNewline;
cqlexec 'select user_name from selfservice_auth.user_auth;' |
    ConvertFrom-Json                                        | 
    foreach {  $userList = $userList + $_.user_name; }      ;
Write-Host "Found $($userList.Count) users " -ForegroundColor green;

foreach ($user in $userList)
{
    Write-Host "Trying with user $user " -ForegroundColor Yellow;
    $resp=&(join-path $PSScriptRoot 'MCFRequest.ps1')  -URI $uri -UserName $user  1> ForAllUsers1.txt 2> ForAllUsers2.txt;;
    if ($resp -ne $null)
    {
        Write-Host "Yay!!!" -ForegroundColor;
        Write-Host $resp -ForegroundColor Green;
        Write-Host $resp.Content -ForegroundColor Green;
    }
    else
    {
        $x=gc .\ForAllUsers2.txt;
        $msg=$x[0].Replace('Invoke-WebRequest : ', '');
        $len=if ($msg.Length -lt 80) { $msg.Length } else { 80 };
        $msg = $msg.Substring(0,$len);
        Write-Host $msg -ForegroundColor Red;
    }

}