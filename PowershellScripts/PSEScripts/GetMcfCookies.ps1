param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = 'testuserbbdev6',#'testuserpaDev1',#"testuser6",
    [parameter(Mandatory=$False, Position=1)][string]$Password = 'Start@123',
    [parameter(Mandatory=$False, Position=2)][string]$Uri  = 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/v1.0/authentication/signin'
)
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1') -UserName $UserName -Password $Password -Uri $Uri;
$uri='http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/v1.0/authentication/mcf-token';
$headers = @{};
$headers.Add('Accept','application/json');
#$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");


$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
$rj=$response.Content | ConvertFrom-Json;
$finalCookie="";
foreach ($cookie in $rj.mcfTokenCookie)
{
    if ($finalCookie.Length -gt 0)
    {
        $finalCookie = $finalCookie + "`n";
    }
    $finalCookie += ($cookie.name + ":" + $cookie.value);
}

return $finalCookie;

