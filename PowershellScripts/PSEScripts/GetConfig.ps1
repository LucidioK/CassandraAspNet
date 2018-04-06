param(   
   [parameter(Mandatory=$true, Position=0)][string]$ConfigurationItem
)
if ($global:configuration -eq $null)
{
    &(join-path $PSScriptRoot 'GetAllConfig.ps1')
}
&(join-path $PSScriptRoot 'PopulatePortConfiguration.ps1');
return $global:configuration[$ConfigurationItem];

