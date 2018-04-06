param(
    [parameter(Mandatory=$False, Position=0)][string]$Script = "TestGetBudgetBill.ps1",
    [parameter(Mandatory=$False, Position=0)][string]$CassandraTable = "budget_bill_activity",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=3)][string]$Password = "$(GetConfig.ps1 'defaultPassword')",
    [parameter(Mandatory=$False, Position=3)][switch]$ForAllUsers = $False
)

$cids=@();
Write-Host "Retrieving contract account ids from table microservices.$CassandraTable... " -ForegroundColor green -NoNewline;
cqlexec "select distinct contract_account_id from microservices.$CassandraTable;" | 
        ConvertFrom-Json                                                          | 
        foreach {  $cids = $cids + $_.contract_account_id; }                      ;
Write-Host "Found $($cids.Count) contract accounts " -ForegroundColor green;


$cids = $cids | Sort-Object | Select-Object -Unique;
$userList = @();
if ($ForAllUsers)
{
    Write-Host "Retrieving users from tabl selfservice_auth.user_auth... " -ForegroundColor green -NoNewline;
    cqlexec 'select user_name from selfservice_auth.user_auth;' |
        ConvertFrom-Json                                        | 
        foreach {  $userList = $userList + $_.user_name; }      ;
    $userList = $userList  | Sort-Object | Select-Object -Unique;
    Write-Host "Found $($userList.Count) users " -ForegroundColor green;
}
else
{
    $userList = $userList + $Username;
}


foreach ($user in $userList)
{
    foreach ($cid in $cids)
    {
        Write-Host "Trying with user $user contract account $cid " -ForegroundColor Yellow -NoNewline;
        $resp=&(join-path $PSScriptRoot $Script) -UserName $user -Password $Password -ContractAccountId $cid 1> x1.txt 2> x2.txt;
        if ($resp -ne $null)
        {
            Write-Host "Yay!!!" -ForegroundColor;
            Write-Host $resp -ForegroundColor Green;
            Write-Host $resp.Content -ForegroundColor Green;
        }
        else
        {
            $x=gc .\x2.txt;
            $msg=$x[0].Replace('Invoke-WebRequest : ', '');
            $len=if ($msg.Length -lt 80) { $msg.Length } else { 80 };
            $msg = $msg.Substring(0,$len);
            Write-Host $msg -ForegroundColor Red;
            if ($msg -ieq 'User Login Not Allowed' -or $msg -ieq 'An error occured validating user credentials.' -or $msf -ieq 'User does not exist.')
            {
                if ($userList.Count -gt 1)
                {
                    Write-Host 'Not a good user, next!' -ForegroundColor Yellow;
                }
                break;
            }
        }
    }
}