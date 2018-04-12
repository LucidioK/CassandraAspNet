param(
    [parameter(Mandatory=$false, Position=0)][string]$Fields = 'System.DateCreated,System.DateModified,System.FileAttributes,System.ItemPathDisplay,System.ItemType,System.ItemTypeText,System.ItemUrl,System.Keywords,System.Size',
    [parameter(Mandatory=$false, Position=1)][string]$SearchFolder = '.',
    [parameter(Mandatory=$false, Position=2)][string]$FileNamePattern  = '*.*',
    [parameter(Mandatory=$false, Position=3)][string]$TextPattern = '*'
)

# in case no search result, see if the folder is indexed by running this as admin:
# "C:\WINDOWS\System32\rundll32.exe" C:\WINDOWS\System32\shell32.dll,Control_RunDLL C:\WINDOWS\System32\srchadmin.dll
function search([string]$sql)
{
    $provider = "provider=search.collatordso;extended properties=’application=windows’;" 
    $connector = new-object system.data.oledb.oledbdataadapter -argument $sql, $provider 
    $dataset = new-object system.data.dataset 
    $result=if ($connector.fill($dataset)) { $dataset.tables[0] }
    return $result;
}

function addFilterIfNeeded([string]$sqlStatement, [string]$filterValue, [string]$filterFormat)
{
    if ($filterValue -ne '%' -and $filterValue -ne '%.%')
    {
        $filter = $filterFormat.Replace('#filter#', $filterValue);
        $sqlStatement = " $sqlStatement AND $filter";
    }
    return $sqlStatement;
}

$internalSearchFolder    = (Resolve-Path $SearchFolder).Path.Replace('\', '/');
$internalFileNamePattern = $FileNamePattern.Replace('*', '%');
$internalTextPattern     = $TextPattern.Replace('*', '%');
$sqlStatement    = "SELECT $Fields FROM SYSTEMINDEX WHERE System.ITEMURL like 'file:$internalSearchFolder/%' ";

$sqlStatement = addFilterIfNeeded $sqlStatement $internalFileNamePattern "System.ITEMURL like '#filter#' ";
$sqlStatement = addFilterIfNeeded $sqlStatement $internalTextPattern "Contains('#filter#')";

return search $sqlStatement;

