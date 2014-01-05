param($installPath, $toolsPath, $package, $project)
$projectPath = Split-Path -Parent $project.FullName

# Function to perform xml transform
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

# Find Cloud project
$ccProj = $project.Object.DTE.Solution.Projects | Where-Object { $_.Kind -eq "{cc5fd16d-436d-48ad-a40c-5a424c6e3e79}" } | Select-Object -First 1
if ($ccProj -eq $null) {
    throw "Couldn't find an Azure Cloud Project in your solution; please follow the instructions at https://github.com/AzureWebFarm.OctopusDeploy"
}

# XDT Transform CSDef file
$csdef = $ccProj.ProjectItems | Where-Object { $_.Name -eq "ServiceDefinition.csdef" }
if ($csdef -eq $null) {
    throw "Couldn't find a ServiceDefinition.csdef file in Azure Cloud Project $($ccProj.Name); please follow the instructions at https://github.com/AzureWebFarm.OctopusDeploy"
}
XmlTransform $csdef.Object.Url (Join-Path $toolsPath "ServiceDefinition.csdef.xdt.xml")

# XDT Transform CSCfg files
$ccProj.ProjectItems |
    Where-Object { $_.Name.EndsWith(".cscfg") } |
    ForEach-Object { XmlTransform $_.Object.Url (Join-Path $toolsPath "ServiceConfiguration.cscfg.xdt.xml") }

# XDT Transform CCProj file
$tempFile = [IO.Path]::GetTempFileName()
$xdt = Get-Content (Join-Path $toolsPath "CloudProject.ccproj.xdt.xml")
$assemblyName = ($project.Properties | Where-Object { $_.Name -eq "AssemblyName" } | Select-Object -First 1).Value
$xdt.Replace("%WebProjectName%", $project.Name).Replace("%WebProjectDir%", $projectPath).Replace("%WebAssemblyName%", $assemblyName) | Set-Content $tempFile
XmlTransform $ccProj.FullName $tempFile

# Set Startup items as copy always
$startupFolder = $project.ProjectItems |
    Where-Object { $_.Name -eq "Startup" } |
    Select-Object -First 1
$startupFolder.ProjectItems |
    ForEach-Object {
        Write-Host "Setting Startup\$($_.Name) as Copy always"
        $_.Properties.Item("CopyToOutputDirectory").Value = [int]1
    }

# Add App.config file with binding redirects
$appConfigProjectItem = $project.ProjectItems | Where-Object { $_.Name -eq "App.config" } | Select-Object -First 1
if ($appConfigProjectItem -eq $null) {
    Write-Host "Opening Web.config to update binding redirects"
    $webConfigProjectItem = $project.ProjectItems | Where-Object { $_.Name -eq "Web.config" } | Select-Object -First 1
    $webConfigPath = ($webConfigProjectItem.Properties | Where-Object { $_.Name -eq "LocalPath" } | Select-Object -First 1).Value
    $webConfig = [xml] (Get-Content $webConfigPath)
    $runtimeNode = $webConfig.configuration.runtime

    Write-Host "Adding an explicit binding redirect for WindowsAzure.Storage"
    $packagesConfigProjectItem = $project.ProjectItems | Where-Object { $_.Name -eq "packages.config" } | Select-Object -First 1
    $packagesConfigPath = ($packagesConfigProjectItem.Properties | Where-Object { $_.Name -eq "LocalPath" } | Select-Object -First 1).Value
    $packagesConfig = [xml] (Get-Content $packagesConfigPath)
    $azureStorageConfig = $packagesConfig.SelectNodes('/packages/package[@id="WindowsAzure.Storage"]') | Select-Object -First 1
    $azureStorageVersion = $azureStorageConfig.version
    $bindingRedirectXml = [xml]('<dependentAssembly>' +
        '<assemblyIdentity name="Microsoft.WindowsAzure.Storage" publicKeyToken="31bf3856ad364e35" culture="neutral" />' +
        '<bindingRedirect oldVersion="0.0.0.0-' + $azureStorageVersion + '" newVersion="' + $azureStorageVersion + '" />' +
      '</dependentAssembly>');
    $bindingRedirect = $bindingRedirectXml.SelectNodes('/dependentAssembly') | Select-Object -First 1
    $bindingRedirectNode = $webConfig.ImportNode($bindingRedirect, $true)
    $runtimeNode.assemblyBinding.AppendChild($bindingRedirectNode)
    $webConfig = [xml] ($webconfig.OuterXml.Replace(' xmlns=""', ''))
    $webConfig.Save($webConfigPath)

    Write-Host "Adding binding redirects to update Web.config"
    Add-BindingRedirect

    Write-Host "Creating an App.config file for use by the RoleEntryPoint with the binding redirects in Web.config"
    $appConfig = [xml]"<?xml version=`"1.0`"?><configuration />"
    $newRuntimeNode = $appConfig.ImportNode($runtimeNode, $true)
    $appConfig.DocumentElement.AppendChild($newRuntimeNode)
    $appConfigPath = Join-Path $projectPath "App.config"
    $appConfig.Save($appConfigPath)
    $project.ProjectItems.AddFromFile($appConfigPath)
}