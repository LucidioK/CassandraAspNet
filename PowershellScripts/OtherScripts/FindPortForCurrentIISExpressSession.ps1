$configOption = "/config:";
[string]$commandLine = (Get-WmiObject Win32_Process -Filter "name = 'iisexpress.exe'" | Select-Object CommandLine).CommandLine;
$pos= $commandLine.IndexOf($configOption);
$commandLine = $commandLine.Substring($pos + $configOption.Length);
[string]$configFile = $commandLine.Split('"')[1];
[xml]$cnf=gc $configFile;
$bindings=$cnf.GetElementsByTagName('binding');
$port=0;
foreach ($binding in $bindings)
{
    if ($binding.bindingInformation.StartsWith('*'))
    {
        $port = $binding.bindingInformation.Split(':')[1];
    }
}

return $port;
