if ($global:configuration -eq $null)
{
    $global:configuration = @{};
}
$port = &(join-path $PSScriptRoot 'FindPortForCurrentIISExpressSession.ps1');
if ($global:configuration.ContainsKey('port'))
{
    $global:configuration.Remove('port');
}
$global:configuration.Add('port', $port);
