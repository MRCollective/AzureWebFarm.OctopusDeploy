$ErrorActionPreference = "Stop"

function Run($command, $arguments) {
    Write-Output "Running $command with $arguments"
    & $command $arguments
    if ($LastExitCode -ne 0) {
        throw "Failed to $command with $arguments"
    }
    Write-Output "Successfully ran $command with $arguments"
}

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
    
    $app = Get-WmiObject -Class Win32_Product -Filter "Name = 'Octopus Deploy Tentacle'"
    if ($app -ne $null) {
        Write-Output "OctopusDeploy Tentacle already installed; uninstalling..."
        $app.Uninstall()
    }

    Write-Output "Installing $TentacleInstallerPath to $tentacleDir"
    Start-Process "msiexec" -ArgumentList "INSTALLLOCATION=""$TentacleDir"" /i ""$TentacleInstallerPath"" /quiet" -Wait

    Write-Output "Executing $TentaclePath create-instance --instance $instance --config $ConfigPath"
    & $TentaclePath create-instance --instance $instance --config $ConfigPath
    if ($LastExitCode -ne 0) {
        throw "Command failed"
    }
    Write-Output "Executing $TentaclePath configure --instance $instance --home $PathRoDeployments --console"
    & $TentaclePath configure --instance $instance --home $PathToDeployments --console
    if ($LastExitCode -ne 0) {
        throw "Command failed"
    }
    Write-Output "Executing $TentaclePath configure --instance $instance --app ""$AppsPath --console"
    & $TentaclePath configure --instance $instance --app ""$AppsPath --console
    if ($LastExitCode -ne 0) {
        throw "Command failed"
    }
    Write-Output "Executing $TentaclePath register-with --instance $instance --server $OctopusServer --environment $TentacleEnvironment --role $TentacleRole --apiKey $OctopusApiKey --comms-style TentacleActive --force --console"
    & $TentaclePath register-with --instance $instance --server $OctopusServer --environment $TentacleEnvironment --role $TentacleRole --apiKey $OctopusApiKey --comms-style TentacleActive --force --console
    if ($LastExitCode -ne 0) {
        throw "Command failed"
    }
    Write-Output "Executing $TentaclePath service --instance $instance --install --start --console"
    & $TentaclePath service --instance $instance --install --start --console
    if ($LastExitCode -ne 0) {
        throw "Command failed"
    }

    exit 0
}
catch
{
    $Host.UI.WriteErrorLine($_)
    exit 1
}
