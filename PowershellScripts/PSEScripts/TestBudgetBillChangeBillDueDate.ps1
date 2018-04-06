param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "200019703517",
    [parameter(Mandatory=$False, Position=2)][string]$NewDay = "2",
    [parameter(Mandatory=$False, Position=3)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')"
    [parameter(Mandatory=$False, Position=4)][string]$Password = "$(GetConfig.ps1 'defaultPassword')"
)
$response=$null;
[string]$uri="http://localhost:$Port/v1.0/account/$ContractAccountId/change-bill-due-date/$NewDay";

$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

Write-Host $uri -ForegroundColor Green;

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers;
$responseContentJson = $response.Content | ConvertTo-Json;
Write-Host $response.StatusCode -ForegroundColor Green;
Write-Host $responseContentJson -ForegroundColor Green;

