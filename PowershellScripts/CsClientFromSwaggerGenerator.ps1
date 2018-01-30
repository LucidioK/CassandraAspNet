
param(
    [parameter(Mandatory=$True, Position=0)][string]$SwaggerJsonFilename
)

function FindExecutableInPath([string]$executableName)
{
    $path=$null;
    $env:Path.Split(';') | foreach {
        if (Test-Path $_ -PathType Container)
        {
            $fn = Join-Path $_ $executableName;
            if (Test-Path $fn)
            {
                $path=$fn;
            }
        }
    }
    return $path;
}

function FindExecutableInPathThrowIfNotFound([string]$executableName, [string]$messageInCaseOfNotFound)
{
    $exec=FindExecutableInPath $executableName;
    if ($exec -eq $null)
    {
        throw $messageInCaseOfNotFound;
    }
    return $exec;
}

function CreateProjectFile([string]$ProjectFileName, [string]$SwaggerJsonFilename)
{
    [string]$ProjectFileContent="
<Project Sdk='Microsoft.NET.Sdk'>                                                                          
  <PropertyGroup>                                                                                          
    <TargetFramework>netcoreapp2.0</TargetFramework>                                                       
    <AssemblyName>App</AssemblyName>                                                                       
  </PropertyGroup>                                                                                         
  <ItemGroup>                                                                                              
    <PackageReference Include='CassandraCSharpDriver'                                 Version='3.4.0.1' /> 
    <PackageReference Include='Microsoft.AspNet.WebApi'                               Version='5.2.3'   /> 
    <PackageReference Include='Microsoft.AspNet.WebApi.Owin'                          Version='5.2.3'   /> 
    <PackageReference Include='Microsoft.AspNetCore.All'                              Version='2.0.3'   /> 
    <PackageReference Include='Microsoft.Extensions.DependencyInjection.Abstractions' Version='2.0.0'   /> 
    <PackageReference Include='Microsoft.Owin.Cors'                                   Version='3.1.0'   /> 
    <PackageReference Include='Microsoft.Owin.Host.SystemWeb'                         Version='3.1.0'   /> 
    <PackageReference Include='Microsoft.Owin.Security.OAuth'                         Version='3.1.0'   /> 
    <PackageReference Include='Microsoft.VisualStudio.Web.CodeGeneration.Design'      Version='2.0.1'   /> 
    <PackageReference Include='Newtonsoft.Json'                                       Version='10.0.1'  /> 
    <PackageReference Include='Ninject'                                               Version='3.3.4'   /> 
    <PackageReference Include='Swashbuckle.AspNetCore'                                Version='1.1.0'   /> 
    <PackageReference Include='System.IdentityModel.Tokens.Jwt'                       Version='5.1.5'   /> 
    <PackageReference Include='Thinktecture.IdentityModel.Core'                       Version='1.4.0'   /> 
  </ItemGroup>                                                                                             
  <ItemGroup>                                                                                              
    <Content Update='$SwaggerJsonFilename'>                                                                
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>                                                
    </Content>                                                                                             
  </ItemGroup>                                                                                             
</Project>     ";
    
    $ProjectFileContent | Out-File $ProjectFileName;
}

function CreateEmptyClass([string]$ClassFileName, [string]$Namespace, [string]$Classname, [string]$OptionalCode="")
{
    [string]$classContent = "
 namespace $Namespace    
 {                   
     using System;
     using System.Collection.Generic;    
     public class $ClassName     
     {                   
 $OptionalCode
     }                   
 }";
     $classContent | Out-File $ClassFileName;
 }

 # nuget install CassandraCSharpDriver

function Main([string]$SwaggerJsonFilename)
{
    if (Test-Path $SwaggerJsonFilename)
    {
        [string]$SwaggerJsonFilenameWithoutExtension=[System.IO.Path]::GetFileNameWithoutExtension($SwaggerJsonFilename);
        [string]$SwaggerJsonFilenameWithoutExtensionUpperCase=$SwaggerJsonFilename.ToUpperInvariant();
        [string]$Namespace                 = $SwaggerJsonFilenameWithoutExtension;
        [string]$ClientNamespace           = ($Namespace) + '.Client';
        [string]$ControllerNamespace       = ($Namespace) + '.Controller';
        [string]$InputSwaggerJsonFile      = ($SwaggerJsonFilename);
        [string]$ClientOutput              = ($Namespace) + 'Client.cs';
        [string]$ControllerClass           = ($Namespace) + 'Controller';
        [string]$ControllerOutput          = ($ControllerClass) + '.cs';
        [string]$ProjectFileName           = ($Namespace) + '.csproj';
        [string]$ClientBaseClass           = ($Namespace) + 'ClientBase';
        [string]$ClientBaseClassConstructor="        public $ClientBaseClass($ConfigurationClass config){ }";
        [string]$ControllerBaseClass       = ($Namespace) + 'ControllerBase';
        [string]$ControllerBaseOutput      = ($ControllerBaseClass) + '.cs';
        [string]$ConfigurationClass        = ($Namespace) + 'Configuration';
        [string]$ConfigurationOutput       = ($ConfigurationClass) + '.cs';
        [string]$ExceptionClass            = ($Namespace) + 'Exception';
        [string]$ClientClassAccessModifier = 'internal';
        [string]$TypeAccessModifier        = 'internal';
        [string]$ContractsNamespace        = ($Namespace) + '.Contracts';
        [string]$ContractsOutput           = '.\Contracts';
        [string]$ClassName                 = ($Namespace) + 'Client';
        [string]$ResponseClass             = ($Namespace) + 'Response';
        [string]$OperationGenerationMode   = 'MultipleClientsFromPathSegments';
        [string]$ServiceSchemes            = 'http,https';
        [string]$ScriptPath                = $PSScriptRoot;
        [string]$CsProjSource              = ($ScriptPath) + 'baseProj.csproj';
        [string]$CsProjDestination         = ($ProjectFileName);

        if ($SwaggerJsonFilenameWithoutExtensionUpperCase -eq 'SWAGGER')
        {
            throw 'Input swagger file should not be named Swagger.json';
        }
        $npm   = FindExecutableInPathThrowIfNotFound 'npm'   'Please install npm from https://nodejs.org/en/download/';
        $nswag = FindExecutableInPathThrowIfNotFound 'nswag' 'Please install nswag with npm install nswag -g';

        &$nswag ( `
            "swagger2csclient", `
            "/Input:$SwaggerJsonFilename", `
            "/ClientBaseClass:$ClientBaseClass", `
            "/ConfigurationClass:$ConfigurationClass", `
            "/GenerateClientClasses", `
            "/GenerateClientInterfaces:true", `
            "/GenerateDtoTypes", `
            "/InjectHttpClient", `
            "/DisposeHttpClient", `
            "/GenerateExceptionClasses", `
            "/ExceptionClass:$ExceptionClass", `
            "/WrapDtoExceptions", `
            "/UseHttpClientCreationMethod", `
            "/UseHttpRequestMessageCreationMethod", `
            "/UseBaseUrl", `
            "/ClientClassAccessModifier:$ClientClassAccessModifier", `
            "/TypeAccessModifier:$TypeAccessModifier", `
            "/GenerateContractsOutput", `
            "/ContractsNamespace:$ContractsNamespace", `
            "/ContractsOutput:$ContractsOutput", `
            "/QueryNullValue", `
            "/ClassName:$ClassName", `
            "/OperationGenerationMode", `
            "/GenerateOptionalParameters", `
            "/WrapResponses", `
            "/WrapResponseMethods", `
            "/GenerateResponseClasses", `
            "/ResponseClass:$ResponseClass", `
            "/Namespace:$ClientNamespace", `
            "/GenerateDefaultValues", `
            "/GenerateDataAnnotations", `
            "/HandleReferences", `
            "/InputSwaggerJsonFile:$SwaggerJsonFilename", `
            "/ServiceHost", `
            "/ServiceSchemes:$ServiceSchemes", `
            "/Output:$ClientOutput", `
            "/AdditionalNamespaceUsages:$ControllerNamespace,$Namespace" );

        CreateProjectFile $ProjectFileName $SwaggerJsonFilename;

        CreateEmptyClass $ClientOutput         $Namespace $ClientBaseClass $ClientBaseClassConstructor;
        CreateEmptyClass $ConfigurationOutput  $Namespace $ConfigurationClass;
        CreateEmptyClass $ControllerBaseOutput $Namespace $ControllerBaseClass;    
    }
    else
    {
        throw "$SwaggerJsonFilename not found.";
    }
}

Main $SwaggerJsonFilename;
