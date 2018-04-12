param(
    [parameter(Mandatory=$false, Position=1)][string]$SearchFolder = '.',
    [parameter(Mandatory=$false, Position=2)][string]$FileNamePattern  = '*.*',
    [parameter(Mandatory=$false, Position=3)][string]$TextPattern = '*',
    [parameter(Mandatory=$false, Position=4)][switch]$JSonResult = $false
)

add-type "public class FoundLine { public int Line {get;set;} public string Text{get;set;}}";

function findTextLines([string]$filePath, [string]$stringToFind)
{
    $lines = gc $filePath;
    $foundLines = @();
    for ($i=0; $i -lt $lines.Length; $i++)
    {
        [string]$line = $lines[$i];
        if ($line.Contains($stringToFind))
        {
            $foundLine = New-Object FoundLine;
            $foundLine.Line = $i + 1;
            $foundLine.Text = $line;
            $foundLines = $foundLines + $foundLine;
        }
    }
    return $foundLines;
}

$rs=&(join-path $PSScriptRoot 'searchFileIndex.ps1') -SearchFolder $SearchFolder -FileNamePattern $FileNamePattern -TextPattern $TextPattern;

$fileNameOnly = $TextPattern -eq '*';

if ($rs -eq $null)
{
    throw "Nothing found.";
}
else
{
    $rsOm = $null;
    if ($fileNameOnly) { $rsOm = @(); } else { $rsOm = @{}; };
    foreach ($r in $rs)
    {
        $filePath = $r.'SYSTEM.ITEMPATHDISPLAY';
        if ($fileNameOnly)
        {
            $rsOm = $rsOm + $filePath;
        }
        else
        {
            $lines = findTextLines $filePath $TextPattern;
            $rsOm.Add($filePath, $lines);
        }
    }
}

if ($JSonResult)
{
    write-output ($rsOm | ConvertTo-Json);
}
else
{
    if ($fileNameOnly)
    {
        foreach ($file in $rsOm)
        {
            Write-Output $file;
        }
    }
    else
    {
        foreach ($key in $rsOm.Keys)
        {
            Write-Output $key;
            $lines = $rsOm[$key];
            foreach ($line in $lines)
            {
                [string]$lineNumber = $line.Line;
                $lineNumber = $lineNumber.PadRight(4);
                $lineText   = $line.Text;
                Write-Host " @${lineNumber}: $lineText";
            }
        }
    }
}
