param(
    [parameter(Mandatory=$True, Position=0)][string]$ConnectionString = "Contact Points = localhost; Port = 9042",
    [parameter(Mandatory=$True, Position=1)][string]$KeySpaceName     = "PetStore",
    [parameter(Mandatory=$True, Position=2)][string]$OutputDirectory  = "c:\temp\PetStore",
    [parameter(Mandatory=$True, Position=2)][string]$OAuthUrl         = "http://some.url"

)
pushd .
&(join-path $PSScriptRoot 'utils.ps1');

[string]$originalPSScriptRoot   = $PSScriptRoot;
[string]$Types2SwaggerCmd       = 'Types2Swagger.cmd';
[string]$CassandraDBAttribute   = 'Cassandra.Mapping.Attributes.TableAttribute';
[string]$BuildConfiguration     = "Debug";
[string]$runtime                = global:GetRuntime;
[string]$netcoreversion         = 'netcoreapp2.0';
[string]$csproj                  =Join-Path $OutputDirectory "$KeySpaceName.csproj";
[string]$publishDirectory       = "bin\$BuildConfiguration\$netcoreversion\$runtime\publish";
[string]$EntitiesOutputDirectory=Join-Path $OutputDirectory "Entities";
[string]$WebOutputDirectory     =Join-Path $OutputDirectory "Web";
[string]$UnitTestOutputDirectory=Join-Path $OutputDirectory "UnitTest";
[string]$TestOutputDirectory    =Join-Path $OutputDirectory "Test";
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
global:CreateDirectoriesIfNotExist ($OutputDirectory, $EntitiesOutputDirectory, $WebOutputDirectory, $UnitTestOutputDirectory, $TestOutputDirectory);
$OutputDirectory                =Resolve-Path $OutputDirectory;

write-host "$CassandraDBtoCSharp $ConnectionString $KeySpaceName $OutputDirectory" -ForegroundColor DarkYellow
# CassandraDBtoCSharp generates the class files, typeDescriptionsFile file and the initialSwaggerFile
&$CassandraDBtoCSharp ($ConnectionString, $KeySpaceName, $OutputDirectory);
if (!($?)) { throw "Error running CassandraDBtoCSharp"; }

cd $OutputDirectory;

Write-Host $GenerateSwaggerStandardOperations $KeySpaceName $initialSwaggerFile $typeDescriptionsFile $swaggerWithOpsFile $OAuthUrl -ForegroundColor DarkYellow;
&$GenerateSwaggerStandardOperations ($KeySpaceName, $initialSwaggerFile, $typeDescriptionsFile, $swaggerWithOpsFile, $OAuthUrl);
if (!($?)) { throw "GenerateSwaggerStandardOperations failed."; }

Write-Host $CreateControllerFromSwaggerWithStandardOperations $swaggerWithOpsFile $ConnectionString 1 24 $csproj $typeDescriptionsFile  -ForegroundColor DarkYellow;
&$CreateControllerFromSwaggerWithStandardOperations ( $swaggerWithOpsFile, $ConnectionString, 1, 24, $csproj, $typeDescriptionsFile);
if (!($?)) { throw "CreateControllerFromSwaggerWithStandardOperations failed."; }

Write-Host 'Done' -ForegroundColor Green;
