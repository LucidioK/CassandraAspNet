param(
    [parameter(Mandatory=$False, Position=0)][switch]$Json = $false
)

Write-Host 'Reading schema from Cassandra, this might take a while...' -ForegroundColor Green;
$allColumns=cqlexec "select keyspace_name, table_name, column_name, type, kind, clustering_order from system_schema.columns;" | ConvertFrom-Json;

Write-Host 'Building a keyspace/tables dictionary...' -ForegroundColor Green;

$kt=@{};
$allColumns | select -Property keyspace_name,table_name -Unique | foreach { 
    $key=$_.keyspace_name; 
    $table=$_.table_name; 
    [System.Collections.ArrayList]$tables=@(); 
    if ($kt.ContainsKey($key)) 
    { 
        $tables=$kt[$key]; 
    } 
    else 
    { 
        $kt.Add($key, @())  1> x1.txt 2> x2.txt; 
    } 


    $tables.Add($table)  1> x1.txt 2> x2.txt; 

    $kt.Remove($key); 
    $kt.Add($key, $tables) 1> x1.txt 2> x2.txt; 
}

$keySpaceNames = $kt.Keys | select -Unique | Sort-Object;
$schema=@{};
Write-Host 'Building Schema...' -ForegroundColor Green;
foreach ($keySpaceName in $keySpaceNames)
{
    Write-Host " Keyspace $keySpaceName" -ForegroundColor DarkGreen;
    $tableNames=$kt[$keySpaceName] | select -Unique | Sort-Object;
    $tableDict=@{};
    foreach ($tableName in $tableNames)
    {
        Write-Host "  Table   $tableName" -ForegroundColor Green;
        $tableColumnsData = $allColumns | where { $_.keyspace_name -eq $keySpaceName -and $_.table_name -eq $tableName };
        $columns=@{};
        foreach ($tableColumnData in $tableColumnsData)
        {
            $tcd = @{};
            $columnName = $tableColumnData.column_name;
            #Write-Host "   Column $columnName" -ForegroundColor Cyan;
            $tcd.Add('type', $tableColumnData.type) 1> x1.txt 2> x2.txt;
            $tcd.Add('kind', $tableColumnData.kind) 1> x1.txt 2> x2.txt;
            $tcd.Add('clustering_order', $tableColumnData.clustering_order) 1> x1.txt 2> x2.txt;

            $columns.Add($columnName, $tcd) 1> x1.txt 2> x2.txt;
        }
        $tableDict.Add($tableName, $columns);
    }
    $schema.Add($keySpaceName, $tableDict);
}
Write-Host 'Done...' -ForegroundColor Green;

if ($Json)
{
    return $schema | ConvertTo-Json -Depth 8;
}
else
{
    return $schema;
}
