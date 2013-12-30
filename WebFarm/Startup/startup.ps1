$ErrorActionPreference = "Stop"

try
{
    $TentacleDownloadPath = "http://download.octopusdeploy.com/octopus/Octopus.Tentacle.2.0.6.950.msi"
    $TentacleLocalDebuggingPath = "C:\Octopus.Tentacle.msi"
    $PathToInstall = $env:PathToInstall
    $TentacleInstallerPath = Join-Path $PathToInstall "Octopus.Tentacle.msi"
    $emulated = $env:ComputeEmulatorRunning
    $TentacleDir = Join-Path $PathToInstall "Tentacle"

    if (Test-Path $TentacleInstallerPath) {
        Write-Output "Already found tentacle installer at $TentacleInstallerPath"
    } elseif (($emulated -eq "true") -and (Test-Path $TentacleLocalDebuggingPath)) {
        Write-Output "Copying $TentacleLocalDebuggingPath to $TentacleInstallerPath"
        cp $TentacleLocalDebuggingPath $TentacleInstallerPath
    } else {
        Write-Output "Downloading $TentacleDownloadPath"
        (new-object System.Net.WebClient).DownloadFile($TentacleDownloadPath, $TentacleInstallerPath)
        Write-Output "Downloaded $TentacleDownloadPath to $TentacleInstallerPath"
    }
    
    $app = Get-WmiObject -Class Win32_Product -Filter "Name = 'Octopus Deploy Tentacle'"
    if ($app -ne $null) {
        Write-Output "OctopusDeploy Tentacle already installed; uninstalling..."
        $app.Uninstall()
    }

    Write-Output "Installing $TentacleInstallerPath to $TentacleDir"
    Start-Process "msiexec" -ArgumentList "INSTALLLOCATION=""$TentacleDir"" /i ""$TentacleInstallerPath"" /quiet" -Wait

    exit 0
}
catch
{
    $Host.UI.WriteErrorLine($_)
    exit 1
}
