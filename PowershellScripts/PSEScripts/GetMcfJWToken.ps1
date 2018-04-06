param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=1)][string]$Password = "$(GetConfig.ps1 'defaultPassword')",
    [parameter(Mandatory=$False, Position=2)][string]$Uri  = "$(GetConfig.ps1 'loadBalancerUrl')/v1.0/authentication/signin"
)
$now = [System.DateTime]::Now;
if ($global:GetMcfJWTTokenLatestUserName -eq $UserName -and $global:GetMcfJWTTokenLatestPassword -eq $Password -and $global:GetMcfJWTTokenLatestExecTime -ne $null -and ($now - $global:GetMcfJWTTokenLatestExecTime).TotalMinues -lt 2)
{
    return $global:GetMcfJWTTokenLatestJwToken;
}
else
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

    $global:GetMcfJWTTokenLatestUserName=$UserName;
    $global:GetMcfJWTTokenLatestPassword=$Password;
    $global:GetMcfJWTTokenLatestJwToken =$jwToken;
    $global:GetMcfJWTTokenLatestExecTime=[System.DateTime]::Now;
    return $jwToken;
}
