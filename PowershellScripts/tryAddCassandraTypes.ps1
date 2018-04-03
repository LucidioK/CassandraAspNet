$dotnetFrameworkDirectory=[System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory();
if (!($env:Path.Contains($dotnetFrameworkDirectory)))
{
    $env:Path = "$env:Path;$dotnetFrameworkDirectory";
}
try
{
    #Add-Type -Path C:\dsv\_LK\CassandraAspNet\cqlExec\bin\Debug\netcoreapp2.0\win10-x64\publish\Cassandra.dll 
    [System.Reflection.Assembly]::LoadFrom('C:\dsv\_LK\CassandraAspNet\cqlExec\bin\Debug\netcoreapp2.0\win10-x64\publish\Cassandra.dll');
}
catch [System.Reflection.ReflectionTypeLoadException]
{
   Write-Host "Message: $($_.Exception.Message)"
   Write-Host "StackTrace: $($_.Exception.StackTrace)"
   Write-Host "LoaderExceptions: $($_.Exception.LoaderExceptions)"
}

