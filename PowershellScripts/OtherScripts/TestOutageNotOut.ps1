param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=1)][string]$NotificationId = "f0b5e065-1a76-4bb6-9dca-44830bd85f8c",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",
    [parameter(Mandatory=$False, Position=3)][string]$Password = "$(GetConfig.ps1 'defaultPassword')"
)

[string]$uri="http://localhost:$portv1.0/outage/notification/not-out?notificationId=$NotificationId"; 
$global:response=$null;
$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

$response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers;
$global:response=$response;
return $response