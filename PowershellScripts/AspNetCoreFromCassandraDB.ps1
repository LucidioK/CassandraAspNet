param(
    [parameter(Mandatory=$True, Position=0)][string]$ConnectionString,
    [parameter(Mandatory=$True, Position=1)][string]$KeySpaceName,
    [parameter(Mandatory=$True, Position=2)][string]$OutputDirectory,
    [parameter(Mandatory=$True, Position=2)][string]$OAuthUrl

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
[string]$dotnet                 = FindExecutableInPathThrowIfNotFound 'dotnet' 'Please install dotnet core from https://www.microsoft.com/net/download/windows#core';
[string]$npm                    = FindExecutableInPathThrowIfNotFound 'npm'   'Please install npm from https://nodejs.org/en/download/';
[string]$nswag                  = FindExecutableInPathThrowIfNotFound 'nswag' 'Please install nswag with npm install nswag -g';
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
&$CassandraDBtoCSharp ($ConnectionString, $KeySpaceName, $OutputDirectory);
if (!($?)) { throw "Error running CassandraDBtoCSharp"; }

cd $OutputDirectory;
&$dotnet ('clean');
write-host "$dotnet publish --self-contained --runtime $runtime --configuration $BuildConfiguration --verbosity Minimal" -ForegroundColor DarkYellow
&$dotnet ('publish',
            '--self-contained',
            '--runtime',       $runtime,
            '--configuration', $BuildConfiguration,
            '--verbosity',     'Minimal');
if (!($?)) { throw "dotnet publish failed."; }


[string]$AppDll = Join-Path $publishDirectory "App.dll";
$AppDll=Resolve-Path $AppDll;
if (!(Test-Path $AppDll)) { throw "$AppDll not found, possibly dotnet publish has failed."; }

$classList = &$ListTypesWithCustomAttribute ($AppDll, $CassandraDBAttribute);
if (!($?)) { throw "ListTypesWithCustomAttribute failed."; }

$classNames=[System.String]::Join(",", $classList);

Write-Host "$nswag types2swagger /Assembly:$AppDll /ClassNames:$classNames /DefaultPropertyNameHandling:CamelCase /DefaultEnumHandling:String /Output:$initialSwaggerFile " -ForegroundColor DarkYellow;
&$nswag ("types2swagger",
            "/Assembly:$AppDll",
            "/ClassNames:$classNames",
            "/DefaultPropertyNameHandling:CamelCase",
            "/DefaultEnumHandling:String",
            "/Output:$initialSwaggerFile");
if (!($?)) { throw "nswag types2swagger failed."; }

timeout /T 5

Write-Host $GenerateSwaggerStandardOperations $KeySpaceName $initialSwaggerFile $typeDescriptionsFile $swaggerWithOpsFile $OAuthUrl -ForegroundColor DarkYellow;
&$GenerateSwaggerStandardOperations ($KeySpaceName, $initialSwaggerFile, $typeDescriptionsFile, $swaggerWithOpsFile, $OAuthUrl);
if (!($?)) { throw "GenerateSwaggerStandardOperations failed."; }

Write-Host $CreateControllerFromSwaggerWithStandardOperations $swaggerWithOpsFile $ConnectionString 1 24 $csproj $typeDescriptionsFile  -ForegroundColor DarkYellow;
&$CreateControllerFromSwaggerWithStandardOperations ( $swaggerWithOpsFile, $ConnectionString, 1, 24, $csproj, $typeDescriptionsFile);
if (!($?)) { throw "CreateControllerFromSwaggerWithStandardOperations failed."; }

Write-Host 'Done' -ForegroundColor Green;
