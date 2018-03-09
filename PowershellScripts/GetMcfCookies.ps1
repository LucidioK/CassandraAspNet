
[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1');
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
        $finalCookie = $finalCookie + "; ";
    }
    $finalCookie += ($cookie.name + "=" + $cookie.value);
}

return $finalCookie;

