if "%1"=="" goto :expl
if "%1"=="-?" goto :expl
if "%1"=="?" goto :expl
if "%1"=="-help" goto :expl
if "%1"=="help" goto :expl

curl --basic --user $2:$3 $1

goto :EOF
:expl
@echo off
echo .
echo httpGetBasicAuth URI userName password
echo .
echo example:
echo httpGetBasicAuth http://some.com/svc barryManilow bm!Rules01
echo .
:EOF