param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "53234"
)
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1');
[string]$uri="http://localhost:$Port/v1.0/account/200009468303/installment/eligibility?installmentPlanType=P15"; 

$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");


$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
