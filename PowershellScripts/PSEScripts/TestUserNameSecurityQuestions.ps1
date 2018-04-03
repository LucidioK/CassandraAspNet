param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "58343",
    [parameter(Mandatory=$False, Position=0)][string]$UserId = "a130d614-9f26-4e0c-9e88-a356edd34953"
)
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1');
[string]$uri="http://localhost:$Port/v1.0/authentication/security-question/user-name-security-questions/$UserId"; 

$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");

Write-Host $uri -ForegroundColor Green;

$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
$responseContentJson = $response.Content | ConvertTo-Json;
Write-Host $response.StatusCode -ForegroundColor Green;
Write-Host $responseContentJson -ForegroundColor Green;