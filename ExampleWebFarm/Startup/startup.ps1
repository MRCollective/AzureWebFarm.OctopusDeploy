$ErrorActionPreference = "Stop"

try
{
    $TentacleDownloadPath = "https://octopusdeploy.com/downloads/latest/OctopusTentacle64"
    $TentacleLocalDebuggingPath = "C:\Octopus.Tentacle.msi"
    $PathToInstall = $env:PathToInstall
    $PathToDeployments = $env:PathToDeployments
    $TentacleInstallerPath = Join-Path $PathToInstall "Octopus.Tentacle.msi"
    $ApplicationsPath = Join-Path $PathToDeployments "Applications"
    $Emulated = $env:ComputeEmulatorRunning
    $TentacleDir = Join-Path $PathToInstall "Tentacle"

    if (Test-Path $TentacleInstallerPath) {
        Write-Output "Already found tentacle installer at $TentacleInstallerPath"
    } elseif (($Emulated -eq "true") -and (Test-Path $TentacleLocalDebuggingPath)) {
        Write-Output "Copying $TentacleLocalDebuggingPath to $TentacleInstallerPath"
        cp $TentacleLocalDebuggingPath $TentacleInstallerPath
    } else {
        Write-Output "Downloading $TentacleDownloadPath"
        (new-object System.Net.WebClient).DownloadFile($TentacleDownloadPath, $TentacleInstallerPath)
        Write-Output "Downloaded $TentacleDownloadPath to $TentacleInstallerPath"
    }
    
    $App = Get-WmiObject -Class Win32_Product -Filter "Name = 'Octopus Deploy Tentacle'"
    if ($App -ne $null) {
        Write-Output "OctopusDeploy Tentacle already installed; uninstalling..."
        $App.Uninstall()
    }

    Write-Output "Installing $TentacleInstallerPath to $TentacleDir"
    Start-Process "msiexec" -ArgumentList "INSTALLLOCATION=""$TentacleDir"" /i ""$TentacleInstallerPath"" /quiet" -Wait

    Write-Output "Setting up ACLs so IIS App Pools have full access to $ApplicationsPath"
    if (-not (Test-Path $ApplicationsPath)) {
        New-Item -type directory -path "$ApplicationsPath"
    }
    $Acl = Get-Acl "$ApplicationsPath"
    $IisAppPoolAccess = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit, ObjectInherit", "None", "Allow")
    $Acl.SetAccessRule($IisAppPoolAccess)
    Set-Acl "$ApplicationsPath" -AclObject $Acl
    Get-ChildItem "$ApplicationsPath" -Recurse -Force | Set-Acl -AclObject $Acl

    if ($Emulated -ne "true") {
        Write-Output "Installing IIS App Initialisation Module for performance"
        PKGMGR.EXE /iu:IIS-ApplicationInit
    }

    exit 0
}
catch
{
    $Host.UI.WriteErrorLine($_)
    exit 1
}
