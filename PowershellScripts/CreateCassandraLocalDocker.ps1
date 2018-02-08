
&(join-path $PSScriptRoot 'utils.ps1');
[string]$docker                 = FindExecutableInPathThrowIfNotFound 'docker' 'Please install &$docker';

Write-Host 'starting the CassandraLocal container, from scratch';
&$docker ('container', 'rm', 'CassandraLocal');

$hasCassandraImage=(docker image ls | foreach { $_ -replace '  +', ';' } | foreach { $_.Split(';')[0] }).Contains('cassandra');
if (!($hasCassandraImage))
{
    &$docker ('pull','cassandra');
}
&$dockercassandrapush ('run', '--name', 'CassandraLocal', '-it', '-d', '-p', '9042:9042', 'cassandra:3.11')
&$docker ('exec', '--privileged', '-it', 'CassandraLocal', 'apt',  'update')

Write-Host 'Opening port 9042 in the docker container.' 
Write-Host 
Write-Host 'Important: answer Y when you are asked to save IPv4 and IPV6 configs.' -ForegroundColor Yellow;


&$docker ('exec', '--privileged', '-it', 'CassandraLocal', 'apt', 'install', '--yes', 'iptables-persistent');
&$docker ('exec', '--privileged', '-it', 'CassandraLocal', 'iptables', '-A', 'INPUT', '-p', 'tcp', '--dport', '9042', '-j', 'ACCEPT');
