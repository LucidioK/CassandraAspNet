param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "49887",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "200028750129",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "roderickdharris",
    [parameter(Mandatory=$False, Position=3)][string]$Password = 'Start@123'
)
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1') -UserName $UserName -Password $Password;
[string]$uri="http://localhost:$port/v1.0/account/$ContractAccountId/budget-bill/unenroll"; 
$Global:response = $null;
$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers;
$Global:response = $response;
return $response;
