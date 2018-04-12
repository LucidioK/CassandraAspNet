$schemaPath=join-path $PSScriptRoot 'CassandraColumnsAllTablesAllKeySpaces.json';
if (!(Test-Path $schemaPath))
{
    &(join-path $PSScriptRoot 'GetCassandraSchema.ps1') -Json | Out-File $schemaPath;
}

function GetProperties($obj)
{
    return Get-Member -InputObject $obj -MemberType NoteProperty | select -Property Name;
}

$schema=gc $schemaPath | ConvertFrom-Json
$keyspaceNames = GetProperties $schema;
foreach ($keyspaceName in $keyspaceNames)
{
    Invoke-Expression ('$keyspace=' + "$schema.$keyspaceName");
    if ($keyspace -ne $null)
    {
        $tableNames = GetProperties $keyspace;
        foreach ($tableName in $tableNames)
        {
            $table = Invoke-Expression "$keyspace.$tableName";
            $columnNames = GetProperties $table;

            foreach ($column in $table)
            {
                if ($column.Name -eq 'contract_account_id')
                {
                    Write-Host "$keyspace.$table.$column.Name";
                }
            }
        }
    }
}