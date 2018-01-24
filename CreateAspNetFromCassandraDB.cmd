if [%1]==[] goto :expl
if [%1]==[-?] goto :expl
if [%1]==[?] goto :expl
if [%1]==[-help] goto :expl
if [%1]==[help] goto :expl
@echo off
set ConnectionString=%1
set KeySpaceName=%2
rem set UpperKeySpaceName=%KeySpaceName%
set csproj=%OutputDirectory%\%KeySpaceName%.csproj
rem call :UpCase UpperKeySpaceName

set OutputDirectory=%~f3
set OAuthUrl=%4
set currentFolder=%cd%
set initialSwaggerFile=%OutputDirectory%\%KeySpaceName%Swagger.json
set swaggerWithOpsFile=%OutputDirectory%\%KeySpaceName%SwaggerWithOperations.json

pushd .

if [%5]==[NOBUILD] goto :AfterBuild

call CreateToolsFolder.cmd
rem call :EchoPause "After CreateToolsFolder"
:AfterBuild
echo .
echo %currentFolder%\tools\CassandraDBtoCSharp.exe %ConnectionString% %KeySpaceName% %OutputDirectory%
echo .
%currentFolder%\tools\CassandraDBtoCSharp.exe %ConnectionString% %KeySpaceName% %OutputDirectory%

rem pause

rem pause

call npm install nswag -g
rem pause

cd "%OutputDirectory%"
dotnet clean
dotnet restore
dotnet build --runtime win10-x64 --configuration "Release"
dotnet publish --self-contained --runtime win10-x64 --configuration "Release"
rem pause

echo .
echo %currentFolder%\tools\GenerateTypes2SwaggerCall.exe %KeySpaceName% bin\x64\Release\netcoreapp2.0\win10-x64\publish\%KeySpaceName%.dll .\Types2Swagger.cmd	
echo .
%currentFolder%\tools\GenerateTypes2SwaggerCall.exe %KeySpaceName% bin\x64\Release\netcoreapp2.0\win10-x64\publish\%KeySpaceName%.dll .\Types2Swagger.cmd	
rem pause

call .\Types2Swagger.cmd

echo .
echo %currentFolder%\tools\GenerateSwaggerStandardOperations.exe %KeySpaceName% %initialSwaggerFile% %OutputDirectory%\typeDescriptions.json %swaggerWithOpsFile% "%OAuthUrl%"
echo .
%currentFolder%\tools\GenerateSwaggerStandardOperations.exe %KeySpaceName% %initialSwaggerFile% %OutputDirectory%\typeDescriptions.json %swaggerWithOpsFile% "%OAuthUrl%"
rem pause

rem call nswag swagger2cscontroller /ControllerBaseClass:Controller /AspNetNamespace:Microsoft.AspNetCore.Mvc /Input:%KeySpaceName%Swagger.json /Output:controller\%KeySpaceName%Controller.cs /ClassName:%KeySpaceName%Controller /Namespace:%UpperKeySpaceName%.Controllers
echo .
echo %currentFolder%\tools\CreateControllerFromSwaggerWithStandardOperations.exe %swaggerWithOpsFile% %ConnectionString% 1 24 %csproj%
echo .
%currentFolder%\tools\CreateControllerFromSwaggerWithStandardOperations.exe %swaggerWithOpsFile% %ConnectionString% 1 24 %csproj%
rem pause

goto :end
:expl
@echo off
echo CreateAspNetFromCassandraDB.cmd CassandraConnectionString KeySpaceName OutputDirectory OAuthURL [NOBUILD]
echo Example:
echo CreateAspNetFromCassandraDB.cmd "Contact Points = localhost; Port = 9042" pse c:\temp\classes https://somesite.com/oauth/authorize/?client_id=CLIENT-ID&redirect_uri=REDIRECT-URI&response_type=token
goto :end

:UpCase
rem FOR %%i IN ("a=A" "b=B" "c=C" "d=D" "e=E" "f=F" "g=G" "h=H" "i=I" "j=J" "k=K" "l=L" "m=M" "n=N" "o=O" "p=P" "q=Q" "r=R" "s=S" "t=T" "u=U" "v=V" "w=W" "x=X" "y=Y" "z=Z") DO CALL SET "%1=%%%1:%%~i%%"
GOTO:EOF

:EchoPause
echo %1
pause
GOTO:EOF

:end