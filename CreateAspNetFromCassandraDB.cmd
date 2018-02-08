rem
rem IMPORTANT: do not edit this script file in Visual Studio,
rem use Notepad++.
rem 
@echo off
set RedBkgYelFgd=[101;93m
set BlkBkgGrnFgd=[92m
set BlkBkgYelFgd=[42m
set BluBkgWhtFgd=[44m
set BlkBkgRedFgd=[91m
set EndColor=[0m
[0m


if [%1]==[] goto :expl
if [%1]==[-?] goto :expl
if [%1]==[?] goto :expl
if [%1]==[-help] goto :expl
if [%1]==[help] goto :expl

call :CheckIfExists dotnet "Please install dotnet core from https://www.microsoft.com/net/download/windows#core"
if not [%errorlevel%]==[0] goto :oops
call :CheckIfExists npm    "Please install npm from https://nodejs.org/en/download/"
if not [%errorlevel%]==[0] goto :oops
call :CheckIfExists nswag  "Please install nswag with npm install nswag -g"
if not [%errorlevel%]==[0] goto :oops

set connectionStringOrLocalSettingsJsonFile=%1
set KeySpaceName=%2
set OutputDirectory=%~f3
rem set UpperKeySpaceName=%KeySpaceName%
set csproj=%OutputDirectory%\%KeySpaceName%.csproj
rem call :UpCase UpperKeySpaceName

set EntitiesOutputDirectory=%OutputDirectory%\Entities
set WebOutputDirectory=%OutputDirectory%\Web
set UnitTestOutputDirectory=%OutputDirectory%\UnitTest
set TestOutputDirectory=%OutputDirectory%\Test
set OAuthUrl=%4
set currentFolder=%cd%
set initialSwaggerFile=%OutputDirectory%\swaggerBase.json
set swaggerWithOpsFile=%OutputDirectory%\swagger.json

if not exist %OutputDirectory%         md %OutputDirectory%
if not exist %EntitiesOutputDirectory% md %EntitiesOutputDirectory%
if not exist %WebOutputDirectory%      md %WebOutputDirectory%
if not exist %UnitTestOutputDirectory% md %UnitTestOutputDirectory%
if not exist %TestOutputDirectory%     md %TestOutputDirectory%

pushd .
echo .
echo %BlkBkgYelFgd% %currentFolder%\tools\CassandraDBtoCSharp.exe %connectionStringOrLocalSettingsJsonFile% %KeySpaceName% %OutputDirectory%  %EndColor%
echo .
%currentFolder%\tools\CassandraDBtoCSharp.exe %connectionStringOrLocalSettingsJsonFile% %KeySpaceName% %OutputDirectory%

if not errorlevel==0 goto :oops
rem pause

rem pause

call npm install nswag -g
if not errorlevel==0 goto :oops
rem pause

cd "%OutputDirectory%"
dotnet clean
dotnet publish --self-contained --runtime win10-x64 --configuration "Release"
if not errorlevel==0 goto :oops
rem pause

echo .
echo %BlkBkgYelFgd% %currentFolder%\tools\GenerateTypes2SwaggerCall.exe %KeySpaceName% bin\x64\Release\netcoreapp2.0\win10-x64\publish\%KeySpaceName%.dll .\Types2Swagger.cmd	 %EndColor%
echo .
%currentFolder%\tools\GenerateTypes2SwaggerCall.exe %KeySpaceName% bin\x64\Release\netcoreapp2.0\win10-x64\publish\App.dll .\Types2Swagger.cmd	
if not errorlevel==0 goto :oops
rem pause

call .\Types2Swagger.cmd

echo .
echo %BlkBkgYelFgd% %currentFolder%\tools\GenerateSwaggerStandardOperations.exe %KeySpaceName% %initialSwaggerFile% %OutputDirectory%\typeDescriptions.json %swaggerWithOpsFile% %OAuthUrl%  %EndColor%
echo .
%currentFolder%\tools\GenerateSwaggerStandardOperations.exe %KeySpaceName% %initialSwaggerFile% %OutputDirectory%\typeDescriptions.json %swaggerWithOpsFile% %OAuthUrl%
if not errorlevel==0 goto :oops
rem pause

rem call nswag swagger2cscontroller /ControllerBaseClass:Controller /AspNetNamespace:Microsoft.AspNetCore.Mvc /Input:%KeySpaceName%Swagger.json /Output:controller\%KeySpaceName%Controller.cs /ClassName:%KeySpaceName%Controller /Namespace:%UpperKeySpaceName%.Controllers
echo .
echo %BlkBkgYelFgd% %currentFolder%\tools\CreateControllerFromSwaggerWithStandardOperations.exe %swaggerWithOpsFile% %connectionStringOrLocalSettingsJsonFile% 1 24 %csproj% %OutputDirectory%\typeDescriptions.json  %EndColor%
echo .
%currentFolder%\tools\CreateControllerFromSwaggerWithStandardOperations.exe %swaggerWithOpsFile% %connectionStringOrLocalSettingsJsonFile% 1 24 %csproj% %OutputDirectory%\typeDescriptions.json
if not errorlevel==0 goto :oops
rem pause

goto :ScriptEnd
:expl
@echo off
echo %BluBkgWhtFgd% CreateAspNetFromCassandraDB.cmd CassandraConnectionString KeySpaceName OutputDirectory OAuthURL %EndColor%
echo %BluBkgWhtFgd% Example: %EndColor%
echo %BluBkgWhtFgd% CreateAspNetFromCassandraDB.cmd "Contact Points = localhost; Port = 9042" PetStore c:\temp\PetStore https://somesite.com/oauth/authorize %EndColor%
echo %EndColor%
goto :ScriptEnd

:CheckIfExists
echo %BlkBkgYelFgd% Checking whether %1 is installed... %EndColor%
where /Q %1 > null
rem Echo with Red background and Yellow foreground.
if not [%errorlevel%]==[0] echo %RedBkgYelFgd% %2 %EndColor%
goto :EOF

:UpCase
rem FOR %%i IN ("a=A" "b=B" "c=C" "d=D" "e=E" "f=F" "g=G" "h=H" "i=I" "j=J" "k=K" "l=L" "m=M" "n=N" "o=O" "p=P" "q=Q" "r=R" "s=S" "t=T" "u=U" "v=V" "w=W" "x=X" "y=Y" "z=Z") DO CALL SET "%1=%%%1:%%~i%%"
GOTO:EOF

:EchoPause
echo %1
pause
GOTO:EOF
:oops
echo %BlkBkgRedFgd% .
echo .
echo Aborting...
echo .
echo . %EndColor%
:ScriptEnd
