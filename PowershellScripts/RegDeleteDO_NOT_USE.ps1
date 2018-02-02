param(
    [parameter(Mandatory=$True, Position=0)][string]$TextToFind
)

&(join-path $PSScriptRoot 'utils.ps1');

function getPrecedingKeyFor($regList, [string]$value)
{
    for ($i=0; $i -lt $regList.Length; $i++)
    {
        if ($regList[$i] -eq $value)
        {
            for ($j=$i-1; $j -gt 0; $j--)
            {
                if (!($regList[$j].StartsWith(' ')))
                {
                    return $regList[$j];
                }
            }
        }
    }
    return $null;
}

$regList = &(join-path $PSScriptRoot 'regFind.ps1')
$selectedKeysToDelete = $regList | Out-GridView -OutputMode Multiple;

foreach ($selectedKeyToDelete in $selectedKeysToDelete)
{
    if ($selectedKeyToDelete.StartsWith('|'))
    {
        $key = getPrecedingKeyFor $regList $selectedKeyToDelete;
        $valueName = $selectedKeyToDelete.Split('|')[1];
        $fullKey = "$KeyName\$key";
        Write-Host "Deleting value $fullKey..." -ForegroundColor Green;
        &$global:reg ('delete', $fullKey, '/v', $valueName, '/reg:32');
        &$global:reg ('delete', $fullKey, '/v', $valueName, '/reg:64');
    }
    else
    {
        $fullKey = "$KeyName\$selectedKeyToDelete";
        Write-Host "Deleting key $fullKey..." -ForegroundColor Green;
        &$global:reg ('delete', $fullKey, '/va', '/f', '/reg:32');
        &$global:reg ('delete', $fullKey, '/va', '/f', '/reg:64');
    }
}


