# Usage example:
# $r=.\MCFRequest.ps1 -URI "https://10.41.53.54:8001/sap/opu/odata/sap/ZERP_UTILITIES_UMC_PSE_SRV/ContractAccounts" -UserName 'testbillduedate1'

param(
    [parameter(Mandatory=$False, Position=0)][string]$URI = "$(GetConfig.ps1 'mcfServer')/sap/opu/odata/sap/ZERP_UTILITIES_UMC_PSE_SRV/ContractAccounts('200009468303')/BudgetBilling",
    [parameter(Mandatory=$False, Position=1)][string]$Method = "GET",
    [parameter(Mandatory=$False, Position=2)][string]$Body = '{"tee":"hee", "moo":"boo"}',
    [parameter(Mandatory=$False, Position=3)][string]$JWTUri  = "$(GetConfig.ps1 'loadBalancerUrl')/v1.0/authentication/signin",
    [parameter(Mandatory=$False, Position=4)][string]$CookiesUri="$(GetConfig.ps1 'loadBalancerUrl')/v1.0/authentication/mcf-token",
    [parameter(Mandatory=$False, Position=5)][string]$UserName = "$(GetConfig.ps1 'defaultUserName')",#'testuserpaDev1',#"testuser6",
    [parameter(Mandatory=$False, Position=6)][string]$Password = "$(GetConfig.ps1 'defaultPassword')",
#    [parameter(Mandatory=$False, Position=7)][string][ValidateSet('Basic','Bearer',$null)]$AuthorizationType = $null,
    [parameter(Mandatory=$False, Position=7)][switch]$ReturnAsJson = $true
)

function HeadersForGet($cookies = @{})
{
    $headers = $cookies;
    $headers.Add('Accept','application/json');
    $headers.Add('Accept-Encoding','gzip, deflate');
    $headers.Add('Accept-Language','en-US,en;q=0.9');
    return $headers;
}

function HeadersForNonGet($cookies = @{})
{
    $headers = HeadersForGet $cookies;
    $headers.Add('X-Requested-With', 'XMLHttpRequest');

    $headers.Add('Content-Type','application/json-patch+json');
    return $headers;
}

function GetCredsJson($userName, $password)
{
    return "{""username"": ""$username"",""password"":""$password""}";
}

function GetJWT($uri, $username, $password)
{
    Write-Host 'Retrieving JWT...' -ForegroundColor Green;
    $headers = HeadersForNonGet;
    $headers.Add('Origin', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com');
    $headers.Add('Referer', 'http://internal-ci-dev-alb-962991584.us-west-2.elb.amazonaws.com/swagger/authentication/');

    $creds = GetCredsJson $username $password;

    $response = Invoke-WebRequest -Uri $Uri -Method Post  -Headers $headers -Body $creds;
    $jwt=($response.Content | ConvertFrom-Json);
    $jwToken=$jwt.jwtAccessToken;
    return $jwToken;
}

function GetCookies($uri, $jwt)
{
    Write-Host 'Retrieving Cookies...' -ForegroundColor Green;
    
    $headers = HeadersForNonGet;
    $headers.Add('Authorization', "$jwt");

    $response = Invoke-WebRequest -Uri $uri -Method Get -Headers $headers;

    $cookies = @{};
    $rj=$response.Content | ConvertFrom-Json;
    foreach ($cookie in $rj.mcfTokenCookie)
    {
        $cookies.Add($cookie.Name, $cookie.Value);
    }

    return $cookies;
}

function MCFGet($uri, $cookies)
{
    Write-Host "GET $uri" -ForegroundColor Green;
    $headers = HeadersForGet $cookies;
    $response = Invoke-WebRequest -Uri $Uri -Method Get  -Headers $headers;
    return $response;
}

function MCFPost($uri, $cookies, $postContent)
{
    Write-Host "POST $uri $postContent" -ForegroundColor Green;
    $headers = HeadersForNonGet $cookies;
    $response = Invoke-WebRequest -Uri $Uri -Method Post -Headers $headers -Body $postContent;
    return $response;
}

function MCFPut($uri, $cookies, $putContent)
{
    Write-Host "PUT $uri $postContent" -ForegroundColor Green;
    $headers = HeadersForNonGet $cookies;
    $response = Invoke-WebRequest -Uri $Uri -Method Put -Headers $headers -Body $putContent;
    return $response;
}

function MCFDelete($uri, $cookies)
{
    Write-Host "DELETE $uri" -ForegroundColor Green;
    $headers = HeadersForNonGet $cookies;
    $response = Invoke-WebRequest -Uri $Uri -Method Delete -Headers $headers;
    return $response;
}

function Ignore-SSLCertificates
{
    $Provider = New-Object Microsoft.CSharp.CSharpCodeProvider;
    $Compiler = $Provider.CreateCompiler();
    $Params = New-Object System.CodeDom.Compiler.CompilerParameters;
    $Params.GenerateExecutable = $false;
    $Params.GenerateInMemory = $true;
    $Params.IncludeDebugInformation = $false;
    $Params.ReferencedAssemblies.Add("System.DLL") > $null
    $TASource=@'
        namespace Local.ToolkitExtensions.Net.CertificatePolicy
        {
            public class TrustAll : System.Net.ICertificatePolicy
            {
                public bool CheckValidationResult(System.Net.ServicePoint sp,System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Net.WebRequest req, int problem)
                {
                    return true;
                }
            }
        }
'@ ;
    $TAResults=$Provider.CompileAssemblyFromSource($Params,$TASource);
    $TAAssembly=$TAResults.CompiledAssembly;
    ## We create an instance of TrustAll and attach it to the ServicePointManager
    $TrustAll = $TAAssembly.CreateInstance("Local.ToolkitExtensions.Net.CertificatePolicy.TrustAll");
    [System.Net.ServicePointManager]::CertificatePolicy = $TrustAll;
}

#$Body     = [System.Web.HttpUtility]::UrlEncode($Body);
#$UserName = [System.Web.HttpUtility]::UrlEncode($UserName);
#$Password = [System.Web.HttpUtility]::UrlEncode($Password);

$jwt = GetJWT $JWTUri $UserName $Password;
$global:jwt=$jwt;
Write-Host $jwt -ForegroundColor DarkGreen

$cookies  = GetCookies $CookiesUri $jwt;
Write-Host ($cookies | ConvertTo-Json) -ForegroundColor DarkGreen

$Method  = $Method.ToUpperInvariant();
$response = $null;
Ignore-SSLCertificates;
switch ($Method)
{
    "GET"    { $response = MCFGet    $URI $cookies; }
    "PUT"    { $response = MCFPut    $URI $cookies $Body; }
    "POST"   { $response = MCFPost   $URI $cookies $Body; }
    "DELETE" { $response = MCFDelete $URI $cookies; }
}

Write-Host "Done..." -ForegroundColor Green;


if ($ReturnAsJson)
{
    add-type "public class ResponseLK { public int StatusCode {get;set;} public object Headers{get;set;} public object Content{get;set;}}";
    $responseObject = New-Object ResponseLK;
    $responseObject.StatusCode = $response.StatusCode;
    $responseObject.Headers = $response.Headers;
    $responseObject.Content = $response.Content;
    $responseObject | ConvertTo-Json;
}
else
{
    $response;
}
