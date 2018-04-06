function Global:prompt 
{
	$time = (Get-Date).ToString("yyyy/MM/dd hh:mm:ss");
	return "PS  $time $PWD`n# ";
} 

Import-Module Microsoft.Powershell.Management;
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 5;
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'PromptOnSecureDesktop' -Value 1;

cd \dsv\tools\scripts
