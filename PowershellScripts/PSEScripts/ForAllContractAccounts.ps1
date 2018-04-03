param(
    [parameter(Mandatory=$False, Position=0)][string]$Script = "TestGetBudgetBill.ps1",
    [parameter(Mandatory=$False, Position=0)][string]$CassandraTable = "budget_bill_activity",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "roderickdharris",
    [parameter(Mandatory=$False, Position=3)][string]$Password = 'Start@123'
)

$cids=@();
cqlexec "select distinct contract_account_id from microservices.$CassandraTable;" | 
        ConvertFrom-Json                                                          | 
        foreach {  $cids = $cids + $_.contract_account_id; }                      ;

$cids = $cids | Sort-Object;


foreach ($cid in $cids)
{
    Write-Host "Trying with $cid " -ForegroundColor Yellow -NoNewline;
    $resp=&(join-path $PSScriptRoot $Script) -UserName $UserName -Password $Password -ContractAccountId $cid 1> x1.txt 2> x2.txt;
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
        Write-Host $msg.Substring(0,$len) -ForegroundColor Red;
    }
}