# Example:
# .\psake.ps1 "default.ps1" "BuildHelloWord" "4.0" 

# Must match parameter definitions for psake.psm1/invoke-psake 
# otherwise named parameter binding fails
param(
  [Parameter(Position=0,Mandatory=0)]
  [string]$buildFile = 'default.ps1',
  [Parameter(Position=1,Mandatory=0)]
  [string[]]$taskList = @(),
  [Parameter(Position=2,Mandatory=0)]
  [string]$framework = '4.0',
  [Parameter(Position=3,Mandatory=0)]
  [switch]$docs = $false,
  [Parameter(Position=4,Mandatory=0)]
  [System.Collections.Hashtable]$parameters = @{},
  [Parameter(Position=5, Mandatory=0)]
  [System.Collections.Hashtable]$properties = @{}
)

$buildNumber = $env:ProjectBuildNumber
$revision = $env:ProjectRevision

if (!$buildNumber) {
    $buildNumber = 0
}

if (!$properties["version"]) {
    $properties["version"] = "3.2.$buildNumber"
}
if (!$properties["revision"]) {
    $properties["revision"] = $revision
}
if (!$properties["nunitconsole"] -and $env:NunitConsolePath) {
    $properties["nunitconsole"] = $env:NunitConsolePath
}

remove-module psake -ea 'SilentlyContinue'
$scriptPath = Split-Path -parent $MyInvocation.MyCommand.path
import-module (join-path $scriptPath psake.psm1)
if (-not(test-path $buildFile))
{
    $buildFile = (join-path $scriptPath $buildFile)
} 

invoke-psake $buildFile $taskList $framework $docs $parameters $properties

if (!$psake.build_success)
{
    echo "psake failed."
    $exitcode = 1
    $host.SetShouldExit($exitcode)
    exit
}

