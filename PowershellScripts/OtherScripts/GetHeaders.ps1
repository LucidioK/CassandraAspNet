param(
    [parameter(Mandatory=$False, Position=0)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=1)][string]$Password = "$(GetConfig.ps1 'defaultPassword')"
)

[string]$jwt=&(join-path $PSScriptRoot 'GetMcfJWToken.ps1') -UserName $UserName -Password $Password;

$headers = @{};
$headers.Add('Accept','application/json');
$headers.Add('Content-Type','application/json-patch+json');
$headers.Add('Accept-Encoding','gzip, deflate');
$headers.Add('Accept-Language','en-US,en;q=0.9');
$headers.Add('Authorization', "$jwt");
return $headers;
