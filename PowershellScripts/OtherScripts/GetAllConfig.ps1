$global:configuration=$null;
$global:configuration=@{};
$configData=gc (join-path $PSScriptRoot 'configuration.json')  | ConvertFrom-Json;
$properties=Get-Member -InputObject $configData -MemberType NoteProperty;
foreach ($property in $properties)
{
    $global:configuration.Add($property.Name, $property.Definition.Split('=')[1]);
}
&(join-path $PSScriptRoot 'PopulatePortConfiguration.ps1');
return $global:configuration;
