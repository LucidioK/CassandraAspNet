param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "57604",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "200019703517",
    [parameter(Mandatory=$False, Position=2)][string]$NewDay = "2",
    [parameter(Mandatory=$False, Position=3)][string]$UserName = 'testuserbbdev7'

)
$response=$null;
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1') -UserName $UserName;
[string]$uri="http://localhost:$Port/v1.0/account/$ContractAccountId/change-bill-due-date/$NewDay";

$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");

Write-Host $uri -ForegroundColor Green;

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers;
$responseContentJson = $response.Content | ConvertTo-Json;
Write-Host $response.StatusCode -ForegroundColor Green;
Write-Host $responseContentJson -ForegroundColor Green;

