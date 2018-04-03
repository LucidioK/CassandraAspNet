param(
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "hansrcooke",
    [parameter(Mandatory=$False, Position=3)][string]$Password = 'Start@123'
)


$cids=cqlexec "select distinct contract_account_id from microservices.contract_account_master;" | ConvertFrom-Json;

foreach ($cid in $cids)
{
    Write-Host $cid -ForegroundColor Green;
    $resp=&(join-path $PSScriptRoot "TestGetPaymentArrangementEligibility.ps1") -UserName $UserName -Password $Password -ContractAccountId $cid.contract_account_id;
    Write-Host $resp;
}