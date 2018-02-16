
param([parameter(Mandatory=$True, Position=0)][string]$localConfigurationJsonFileName)

if (!(Test-Path $localConfigurationJsonFileName))
{
    throw "$localConfigurationJsonFileName not found.";
}

$lj                    = gc $localConfigurationJsonFileName | Out-String | ConvertFrom-Json;

$ipAddressList         = $lj.CassandraSettings.Hosts | foreach { $_.IpAddress };

$ipAddresses           = [System.String]::Join(',', $ipAddressList);
$Port                  = $lj.CassandraSettings.Port;
$ClusterUser           = $lj.CassandraSettings.ClusterUser;
$ClusterPassword       = $lj.CassandraSettings.ClusterPassword;
$ConsistencyLevel      = $lj.CassandraSettings.ConsistencyLevel;
$UseClusterCredentials = $lj.CassandraSettings.UseClusterCredentials;
$UseSSL                = $lj.CassandraSettings.UseSSL;
$UseQueryOptions       = $lj.CassandraSettings.UseQueryOptions;
$MaxConnectionsPerHost = $lj.CassandraSettings.MaxConnectionsPerHost;

"Contact Points=$ipAddresses;Port=$Port;UserName=$ClusterUser;Password=$ClusterPassword";

