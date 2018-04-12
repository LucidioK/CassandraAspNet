param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "200028750129",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=3)][string]$Password = "$(GetConfig.ps1 'defaultPassword')"
)

[string]$uri="http://localhost:$port/v1.0/account/$ContractAccountId/budget-bill"; 
$global:response=$null;
$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
$global:response=$response;
return $response