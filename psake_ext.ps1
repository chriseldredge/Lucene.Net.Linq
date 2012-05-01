function Generate-Assembly-Info
{
param(
	[string]$version,
	[string]$revision,
	[string]$file = $(throw "file is a required parameter.")
)
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersionAttribute(""$($version.Split('-')[0])"")]
[assembly: AssemblyInformationalVersionAttribute(""$version / $revision"")]
[assembly: AssemblyFileVersionAttribute(""$($version.Split('-')[0])"")]
"

	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file"
	Write-Output $asmInfo > $file
}