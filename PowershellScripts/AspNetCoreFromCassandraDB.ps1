param(
    [parameter(Mandatory=$True, Position=0)][string]$ConnectionStringOrLocalConfigurationJsonFile = "Contact Points = localhost; Port = 9042",
    [parameter(Mandatory=$True, Position=1)][string]$KeySpaceName     = "PetStore",
    [parameter(Mandatory=$True, Position=2)][string]$OutputDirectory  = "c:\temp\PetStore",
    [parameter(Mandatory=$True, Position=2)][string]$OAuthUrl         = "http://some.url"

)
pushd .
&(join-path $PSScriptRoot 'utils.ps1');

if (!($ConnectionStringOrLocalConfigurationJsonFile.Contains('=')) -and (Test-Path $ConnectionStringOrLocalConfigurationJsonFile))
{
    $ConnectionStringOrLocalConfigurationJsonFile = Resolve-Path $ConnectionStringOrLocalConfigurationJsonFile;
    Write-Host "Using configuration file $ConnectionStringOrLocalConfigurationJsonFile." -ForegroundColor Green;
}
else
{
    Write-Host "Using connection string '$ConnectionStringOrLocalConfigurationJsonFile'." -ForegroundColor Green;
}

if (Test-Path $OutputDirectory)
{
    global:moveDirectoryToRecycleBin $OutputDirectory;
}
[string]$webAppZip              = Join-Path $PSScriptRoot "WebApp.zip";
global:CreateDirectoriesIfNotExist ($OutputDirectory);
$OutputDirectory =Resolve-Path $OutputDirectory;
global:Unzip $webAppZip $OutputDirectory;

$OutputDirectory =Join-Path $OutputDirectory "WebApp";

[string]$originalPSScriptRoot   = $PSScriptRoot;
[string]$Types2SwaggerCmd       = 'Types2Swagger.cmd';
[string]$CassandraDBAttribute   = 'Cassandra.Mapping.Attributes.TableAttribute';
[string]$BuildConfiguration     = "Debug";

[string]$runtime                = global:GetRuntime;
[string]$netcoreversion         = 'netcoreapp2.0';
[string]$csproj                  =Join-Path $OutputDirectory "webapp.csproj";
[string]$publishDirectory       = "bin\$BuildConfiguration\$netcoreversion\$runtime\publish";

[string]$currentFolder          =pwd;
[string]$initialSwaggerFile     =Join-Path $OutputDirectory 'swaggerBase.json';
[string]$swaggerWithOpsFile     =Join-Path $OutputDirectory 'swagger.json';
[string]$typeDescriptionsFile   =Join-Path $OutputDirectory 'typeDescriptions.json';
[string]$ToolsDirectory         =Join-Path $currentFolder 'tools';
[string]$CassandraDBtoCSharp    =Join-Path $ToolsDirectory 'CassandraDBtoCSharp.exe';
[string]$ListTypesWithCustomAttribute =Join-Path $ToolsDirectory 'ListTypesWithCustomAttribute.exe';
[string]$GenerateSwaggerStandardOperations =Join-Path $ToolsDirectory 'GenerateSwaggerStandardOperations.exe';
[string]$CreateControllerFromSwaggerWithStandardOperations =Join-Path $ToolsDirectory 'CreateControllerFromSwaggerWithStandardOperations.exe';

global:AddToPathIfNeeded $ToolsDirectory;


write-host "$CassandraDBtoCSharp $ConnectionStringOrLocalConfigurationJsonFile $KeySpaceName $OutputDirectory" -ForegroundColor DarkYellow
# CassandraDBtoCSharp generates the class files, typeDescriptionsFile file and the initialSwaggerFile
&$CassandraDBtoCSharp ($ConnectionStringOrLocalConfigurationJsonFile, $KeySpaceName, $OutputDirectory);
if (!($?)) { throw "Error running CassandraDBtoCSharp"; }

cd $OutputDirectory;

Write-Host $GenerateSwaggerStandardOperations $KeySpaceName $initialSwaggerFile $typeDescriptionsFile $swaggerWithOpsFile $OAuthUrl -ForegroundColor DarkYellow;
&$GenerateSwaggerStandardOperations ($KeySpaceName, $initialSwaggerFile, $typeDescriptionsFile, $swaggerWithOpsFile, $OAuthUrl);
if (!($?)) { throw "GenerateSwaggerStandardOperations failed."; }

Write-Host $CreateControllerFromSwaggerWithStandardOperations $swaggerWithOpsFile $ConnectionStringOrLocalConfigurationJsonFile 1 24 $csproj $typeDescriptionsFile  -ForegroundColor DarkYellow;
&$CreateControllerFromSwaggerWithStandardOperations ( $swaggerWithOpsFile, $ConnectionStringOrLocalConfigurationJsonFile, 1, 24, $csproj, $typeDescriptionsFile);
if (!($?)) { throw "CreateControllerFromSwaggerWithStandardOperations failed."; }

Write-Host 'Done' -ForegroundColor Green;
