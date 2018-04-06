param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=0)][string]$UserId = "a130d614-9f26-4e0c-9e88-a356edd34953"
)
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1');
[string]$uri="http://localhost:$Port/v1.0/authentication/security-question/user-name-security-questions/$UserId"; 

$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

Write-Host $uri -ForegroundColor Green;

$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
$responseContentJson = $response.Content | ConvertTo-Json;
Write-Host $response.StatusCode -ForegroundColor Green;
Write-Host $responseContentJson -ForegroundColor Green;