param($installPath, $toolsPath, $package, $project)

$projectPath = Split-Path -Parent $project.FullName

Add-Type -Path ($project.Object.References | Where-Object { $_.Name -eq "Microsoft.Web.XmlTransform" }).Path
function XmlTransform([string] $file, [string] $transformFile) {

    Write-Host "Backing up $file to $file.backup"
    cp $file "$file.backup"

    Write-Host "Applying xdt transformation to $file using $transformFile"
    $doc = New-Object Microsoft.Web.XmlTransform.XmlTransformableDocument
    $doc.PreserveWhiteSpace = $true
    $doc.Load($file)

    $trn = New-Object Microsoft.Web.XmlTransform.XmlTransformation($transformFile)

    if ($trn.Apply($doc))
    {
        $doc.Save($file)
    }
    else
    {
        throw "Failed to transform $file using $transformFile"
    }
}

$ccProj = $project.Object.DTE.Solution.Projects | Where-Object { $_.Kind -eq "{cc5fd16d-436d-48ad-a40c-5a424c6e3e79}" } | Select-Object -First 1
if ($ccProj -eq $null) {
    throw "Couldn't find an Azure Cloud Project in your solution; please follow the instructions at https://github.com/AzureWebFarm.OctopusDeploy"
}

$csdef = $ccProj.ProjectItems | Where-Object { $_.Name -eq "ServiceDefinition.csdef" }
if ($csdef -eq $null) {
    throw "Couldn't find a ServiceDefinition.csdef file in Azure Cloud Project $($ccProj.Name); please follow the instructions at https://github.com/AzureWebFarm.OctopusDeploy"
}
XmlTransform $csdef.Object.Url (Join-Path $toolsPath "ServiceDefinition.csdef.xdt.xml")

$csdef = $ccProj.ProjectItems |
    Where-Object { $_.Name.EndsWith(".cscfg") } |
    ForEach-Object { XmlTransform $_.Object.Url (Join-Path $toolsPath "ServiceConfiguration.cscfg.xdt.xml") }

$toolAppConfig = Join-Path $toolsPath "App.config"
Write-Host "Adding $toolAppConfig to $($project.Name) project"
$projAppConfig = Join-Path $projectPath "App.config"
if (-not (Test-Path $projAppConfig)) {
    Copy-Item $toolAppConfig $projAppConfig
    $project.ProjectItems.AddFromFile($projAppConfig)
}

Write-Host $toolsPath
$tempFile = [IO.Path]::GetTempFileName()
$xdt = Get-Content (Join-Path $toolsPath "CloudProject.ccproj.xdt.xml")
$assemblyName = ($project.Properties | Where-Object { $_.Name -eq "AssemblyName" } | Select-Object -First 1).Value
$xdt.Replace("%WebProjectName%", $project.Name).Replace("%WebProjectDir%", $projectPath).Replace("%WebAssemblyName%", $assemblyName) | Set-Content $tempFile
XmlTransform $ccProj.FullName $tempFile

$startupFolder = $project.ProjectItems |
    Where-Object { $_.Name -eq "Startup" } |
    Select-Object -First 1
$startupFolder.ProjectItems |
    ForEach-Object {
        Write-Host "Setting Startup\$($_.Name) as Copy always"
        $_.Properties.Item("CopyToOutputDirectory").Value = [int]1
    }
