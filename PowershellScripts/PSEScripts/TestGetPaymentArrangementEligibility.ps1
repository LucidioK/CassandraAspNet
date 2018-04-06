param(
    [parameter(Mandatory=$False, Position=0)][string]$Port = "$(GetConfig.ps1 'port')",
    [parameter(Mandatory=$False, Position=1)][string]$ContractAccountId = "300024827541",
    [parameter(Mandatory=$False, Position=2)][string]$UserName = 'donaldmcconnell',
    [parameter(Mandatory=$False, Position=3)][string]$Password = 'Start@123'
)

[string]$uri="http://localhost:$port/v1.0/account/$ContractAccountId/installment/eligibility?installmentPlanType=P15"; 

$headers = &(join-path $PSScriptRoot 'GetHeaders.ps1') -UserName $UserName -Password $Password;

$response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
return $response
