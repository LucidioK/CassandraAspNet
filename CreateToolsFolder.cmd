
del tools\*.* /s /q > null
md tools
pushd .

dir *.csproj /s /b > csprojs.lst
for /F "eol=; tokens=* delims=, " %%f in (csprojs.lst) do (
	cd %%~pf
	dotnet clean > null
	rem dotnet restore 
	rem dotnet build --runtime win10-x64 --configuration "Debug"
	dotnet publish --self-contained --runtime win10-x64 --configuration "Debug" --verbosity Minimal
)

popd

dir publish /ad /s /b > publishoutput.lst
for /F "eol=; tokens=* delims=, " %%f in (publishoutput.lst) do (
	robocopy %%f tools /NP /MT:32> null
)

copy *.cmd tools
copy *.ps1 tools

Call :AddToolsFolderToPath tools

goto :End 

:AddToolsFolderToPath
	set PATH=%PATH%;%~f1
	exit /b

:End
