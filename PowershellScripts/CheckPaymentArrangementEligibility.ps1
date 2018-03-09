param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = 'testuserpaDev1',#"testuser6",
    [parameter(Mandatory=$False, Position=1)][string]$Password = 'Start@123',
    [parameter(Mandatory=$False, Position=2)][string]$Uri  = 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/v1.0/authentication/signin'
)

[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1');
[string]$uri="http://10.41.53.54:8002/sap/opu/odata/sap/ZERP_UTILITIES_UMC_PSE_SRV/PaymentArrangementSet?saml2=disabled"; 
[string]$cookies=&(join-path $PSScriptRoot 'GetMcfCookies.ps1');
$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
#$headers.Add('Authorization', "$jwt");
$headers.Add('Cookies',$cookies);
$content='{"TestRun":"X","ContractAccountID":"200009468303","InstallmentPlanType":"P15","PaymentArrangementNav":[]}';

$response = Invoke-WebRequest -Uri $Uri -Method Post -Body $content  -Headers $headers -;
Write-Host $response.StatusCode

return $response.Content;