@echo off
del tools\*.* /s /q > null
pushd .
echo Finding bin and obj directories to delete...
dir bin /ad /b /s >  todelete.lst
dir obj /ad /b /s >> todelete.lst
for /F "eol=; tokens=* delims=, " %%f in (todelete.lst) do (
        echo Deleting %%f\*.* 
        del %%f\*.* /s /q > null
)

del null
popd