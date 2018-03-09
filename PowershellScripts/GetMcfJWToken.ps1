param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = 'testuserpaDev1',#"testuser6",
    [parameter(Mandatory=$False, Position=1)][string]$Password = 'Start@123',
    [parameter(Mandatory=$False, Position=2)][string]$Uri  = 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/v1.0/authentication/signin'
)


$headers = @{};
$headers.Add('Accept','application/json');
$headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Referer', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/swagger/authentication/');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');

$creds = "{""username"": ""$UserName"",""password"":""$Password""}";

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers -Body $creds;
$jwt=($response.Content | ConvertFrom-Json);
$jwToken=$jwt.jwtAccessToken;

return $jwToken;

