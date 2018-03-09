if "%1"=="" goto :expl
if "%1"=="-?" goto :expl
if "%1"=="?" goto :expl
if "%1"=="-help" goto :expl
if "%1"=="help" goto :expl

echo curl --basic --user %2:%3 %1
curl --basic --user %2:%3 %1 > out.edmx
"%windir%\Microsoft.NET\Framework\v3.5\datasvcutil.exe" /language:CSharp /in:out.edmx /out:%4
goto :EOF
:expl
@echo off
echo .
echo generateDataServicesClassesFromURI.cmd URI userName password outputFile
echo .
echo example:
echo generateDataServicesClassesFromURI.cmd http://some.com/svc barryManilow bm!Rules01 c:\temp\someServiceClasses.cs 
echo .
:EOF