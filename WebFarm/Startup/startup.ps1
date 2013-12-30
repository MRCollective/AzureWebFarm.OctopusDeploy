$ErrorActionPreference = "Stop"

try
{
    $PathToInstall = $env:PathToInstall
    $PathToDeployments = $env:PathToDeployments
    $OctopusServer = $env:OctopusServer
    $OctopusApiKey = $env:OctopusApiKey
    $TentacleEnvironment = $env:TentacleEnvironment
    $TentacleRole = $env:TentacleRole

    $TentacleInstallerPath = Join-Path $PathToInstall "Octopus.Tentacle.msi"
    $TentacleDir = Join-Path $PathToInstall "Tentacle"
    $TentaclePath = Join-Path (Join-Path $TentacleDir "Agent") "Tentacle.exe"
    $ConfigPath = Join-Path $PathToInstall "Tentacle.config"
    $AppsPath = Join-Path $PathToDeployments "Applications"
    $instance = "Tentacle"
    $InstanceArg = "--instance ""$instance"""

    if (Test-Path $TentacleInstallerPath) {
        Write-Output "Already found tentacle installer at $TentacleInstallerPath"
    } else {
        $TentacleDownloadPath = "http://download.octopusdeploy.com/octopus/Octopus.Tentacle.2.0.6.950.msi"
        Write-Output "Downloading $TentacleDownloadPath"
        (new-object System.Net.WebClient).DownloadFile($TentacleDownloadPath, $TentacleInstallerPath)
        Write-Output "Downloaded $TentacleDownloadPath to $TentacleInstallerPath"
    }
    #cp c:\users\robert\desktop\Octopus.Tentacle.msi $TentacleInstallerPath
    
    $app = Get-WmiObject -Class Win32_Product -Filter "Name = 'Octopus Deploy Tentacle'"
    if ($app -ne $null) {
        Write-Output "OctopusDeploy Tentacle already installed; uninstalling..."
        $app.Uninstall()
    }

    Write-Output "Installing $TentacleInstallerPath to $tentacleDir"
    Start-Process "msiexec" -ArgumentList "INSTALLLOCATION=""$TentacleDir"" /i ""$TentacleInstallerPath"" /quiet" -Wait

    exit 0
}
catch
{
    $Host.UI.WriteErrorLine($_)
    exit 1
}
