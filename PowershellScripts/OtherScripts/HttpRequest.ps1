

param(
    [parameter(Mandatory=$False, Position=0)][string]$Uri = "$(GetConfig.ps1 'loadBalancerUrl')/v1.0/account/220007375482/budget-bill",
    [parameter(Mandatory=$False, Position=1)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=2)][string]$Password = "$(GetConfig.ps1 'defaultPassword')",
    [parameter(Mandatory=$False, Position=3)][string]$Method = "GET",
    [parameter(Mandatory=$False, Position=4)][string]$Body = '{"tee":"hee", "moo":"boo"}',
    [parameter(Mandatory=$False, Position=5)][string]$AuthType = 'Basic' # or 'Bearer' or 'None' or 'Cookies'
)

if ($AuthType.ToUpperInvariant() -ne 'BASIC' -and $AuthType.ToUpperInvariant() -ne 'BEARER' -and $AuthType.ToUpperInvariant() -ne 'NONE' -and $AuthType.ToUpperInvariant() -ne 'COOKIE')
{
    throw "AuthType must be None, Basic, Cookie or Bearer, but it was provided $AuthType";
}

function getMcfJWToken($UserName = "$(GetConfig.ps1 'defaultUserName')",$Password = "$(GetConfig.ps1 'defaultPassword')", $Uri  = "$(GetConfig.ps1 'loadBalancerUrl')/v1.0/authentication/signin")
{

    $response = $null;
    $headers = @{};
    $headers.Add('Accept','application/json');
    $headers.Add('Origin', "$(GetConfig.ps1 'loadBalancerUrl')");
    $headers.Add('Content-Type','application/json-patch+json');
    $headers.Add('Referer', "$(GetConfig.ps1 'loadBalancerUrl')/swagger/authentication/");
    $headers.Add('Accept-Encoding','gzip, deflate');
    $headers.Add('Accept-Language','en-US,en;q=0.9');

    $creds = "{""username"": ""$UserName"",""password"":""$Password""}";

    $response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers -Body $creds;
    $jwt=($response.Content | ConvertFrom-Json);
    $jwToken=$jwt.jwtAccessToken;

    return $jwToken;
}


function injectCookies($headersOut, [string]$username, [string]$password)
{

    [string]$jwt=getMcfJWToken $username $password ;
    $uri="$(GetConfig.ps1 'loadBalancerUrl')/v1.0/authentication/mcf-token";
    $headers = @{};
    $headers.Add('Accept','application/json');
    $headers.Add('Content-Type','application/json-patch+json');
    $headers.Add('Accept-Encoding','gzip, deflate');
    $headers.Add('Accept-Language','en-US,en;q=0.9');
    $headers.Add('Authorization', "$jwt");


    $response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
    $rj=$response.Content | ConvertFrom-Json;
    $finalCookie="";
    foreach ($cookie in $rj.mcfTokenCookie)
    {
        $headersOut.Add($cookie.name, $cookie.value);
    }
    return $headersOut;
}

[string]$creds = '';

$headers = @{};

if ($AuthType.ToUpperInvariant() -eq 'BASIC')
{
    $pair = $UserName + ':' + $Password;
    $creds = "Basic "+ [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair));
}
if ($AuthType.ToUpperInvariant() -eq 'BEARER')
{
    $creds = "Bearer " + (getMcfJWToken $UserName $Password);
}
if ($AuthType.ToUpperInvariant() -eq 'Cookie')
{
    $headers = injectCookies $headers $UserName $Password;
}


$global:response=$null;
$Method = $Method.ToUpperInvariant();

$headers.Add('Accept','application/json');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
if ($creds.Length > 0)
{
    $headers.Add('Authorization', $creds);
}
if ($Method -eq "GET")
{
    $response = Invoke-WebRequest -Uri $Uri -Method $Method  -Headers $headers;
}
else
{
    $headers.Add("X-Requested-With","XMLHttpRequest");
    $response = Invoke-WebRequest -Uri $Uri -Method $Method  -Headers $headers -Body $Body;
}
$global:response=$response;
return $response
