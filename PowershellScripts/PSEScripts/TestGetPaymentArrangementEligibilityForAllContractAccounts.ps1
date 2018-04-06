param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=1)][string]$Password = "$(GetConfig.ps1 'defaultPassword')"
)


$cids=cqlexec "select distinct contract_account_id from microservices.contract_account_master;" | ConvertFrom-Json;

foreach ($cid in $cids)
{
    Write-Host $cid -ForegroundColor Green;
    $resp=&(join-path $PSScriptRoot "TestGetPaymentArrangementEligibility.ps1") -UserName $UserName -Password $Password -ContractAccountId $cid.contract_account_id;
    Write-Host $resp;
}