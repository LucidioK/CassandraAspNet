param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "200013404187",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')"
)
[string]$uri="http://localhost:$port/v1.0/account/$ContractAccountId/ebill-terms-and-conditions"; 

$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers;
$global:response=$response;