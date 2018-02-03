param(
    [parameter(Mandatory=$True, Position=0)][string]$cqlStatement,
    [parameter(Mandatory=$False, Position=1)][string]$CassandraDockerContainer='CassandraLocal'
)
&(join-path $PSScriptRoot 'utils.ps1');
[string]$docker                 = FindExecutableInPathThrowIfNotFound 'docker' 'Please install docker';
global:StartDockerContainerIfNeeded $CassandraDockerContainer;
&$docker ('exec', '--privileged', '-it', $CassandraDockerContainer, 'cqlsh', '-e', $cqlStatement);
