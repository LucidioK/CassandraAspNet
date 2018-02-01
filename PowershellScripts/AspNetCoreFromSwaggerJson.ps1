param(
    [parameter(Mandatory=$True, Position=0)][string]$SwaggerFile,
    [parameter(Mandatory=$True, Position=1)][string]$OutputDirectory,
    [parameter(Mandatory=$True, Position=2)][string]$OAuthUrl
)
pushd .
&(join-path $PSScriptRoot 'utils.ps1');

[string]$originalPSScriptRoot   = $PSScriptRoot;
[string]$KeySpaceName           = [System.IO.Path]::GetFileNameWithoutExtension($SwaggerFile);
[string]$currentFolder          =pwd;
[string]$ToolsDirectory         =Join-Path $currentFolder 'tools';
[string]$TestCassandraConnectionString = Join-Path $ToolsDirectory 'TestCassandraConnectionString.exe';
[string]$CreateCassandraDBFromSwaggerJson  = Join-Path $ToolsDirectory 'CreateCassandraDBFromSwaggerJson.exe';
[string]$CassandraConnectionString = 'Contact Points = localhost; Port = 9042';

global:AddToPathIfNeeded $ToolsDirectory;
global:CreateDirectoriesIfNotExist ($OutputDirectory);
$OutputDirectory                =Resolve-Path $OutputDirectory;

&$TestCassandraConnectionString ($CassandraConnectionString);
if (!($?)) 
{
    &(Join-Path $PSScriptRoot 'CreateCassandraLocalDocker.ps1');
}

&$CreateCassandraDBFromSwaggerJson ($CassandraConnectionString, $SwaggerFile); 
if (!($?)) { throw "CreateCassandraDBFromSwaggerJson failed."; }

&(Join-Path $PSScriptRoot 'AspNetCoreFromCassandraDB.ps1') -ConnectionString $CassandraConnectionString -KeySpaceName $KeySpaceName -OutputDirectory $OutputDirectory -OAuthUrl $OAuthUrl;
Write-Host 'Done' -ForegroundColor Green;
