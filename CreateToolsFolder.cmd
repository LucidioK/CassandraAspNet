
del tools\*.* /s /q
md tools
pushd .

dir *.csproj /s /b > csprojs.lst
for /F "eol=; tokens=* delims=, " %%f in (csprojs.lst) do (
	cd %%~pf
	dotnet clean
	dotnet restore 
	dotnet build --runtime win10-x64 --configuration "Debug"
	dotnet publish --self-contained --runtime win10-x64 --configuration "Debug"
)

popd

dir publish /ad /s /b > publishoutput.lst
for /F "eol=; tokens=* delims=, " %%f in (publishoutput.lst) do (
	copy %%f\*.* tools
)

copy *.cmd tools

Call :AddToolsFolderToPath tools

goto :End 

:AddToolsFolderToPath
	set PATH=%PATH%;%~f1
	exit /b

:End
