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

    Write-Output "Creating service 'OctopusDeploy Tentacle'"
    if ((Get-Service -Name "OctopusDeploy Tentacle" -ErrorAction SilentlyContinue) -ne $null) {
        sc.exe delete "OctopusDeploy Tentacle"
    }
    New-Service -Name "OctopusDeploy Tentacle" -DisplayName "OctopusDeploy Tentacle" -Description "Octopus Deploy: Tentacle deployment agent" -StartupType Automatic -BinaryPathName ('"' + $TentaclePath + '" run --instance="' + $instance + '"')

    Write-Output "Installing $TentacleInstallerPath to $tentacleDir"
    Start-Process "msiexec" -ArgumentList "INSTALLLOCATION=""$TentacleDir"" /i ""$TentacleInstallerPath"" /quiet" -Wait

    #Write-Output "Executing $TentaclePath create-instance --instance $instance --config $ConfigPath"
    #& $TentaclePath create-instance --instance $instance --config $ConfigPath
    #if ($LastExitCode -ne 0) {
    #    throw "Command failed"
    #}
    "<?xml version='1.0' encoding='UTF-8' ?><octopus-settings></octopus-settings>" | Out-File $ConfigPath -Force
    New-Item -Path HKLM:\Software\Octopus\Tentacle -Name $instance –Force
    Set-ItemProperty -Path HKLM:\Software\Octopus\Tentacle\$instance -Name "ConfigurationFilePath" -Value "$ConfigPath"


    #Write-Output "Executing $TentaclePath configure --instance $instance --home $PathRoDeployments --console"
    #& $TentaclePath configure --instance $instance --home $PathToDeployments --console
    #if ($LastExitCode -ne 0) {
    #    throw "Command failed"
    #}
    [xml]$xml = Get-Content $ConfigPath
    $setting = $xml.CreateElement("set")
    $setting.SetAttribute("key", "Octopus.Home")
    $setting.InnerText = "$PathToDeployments"
    $xml.SelectSingleNode("./octopus-settings").AppendChild($setting)
    $xml.Save("$ConfigPath")


    #Write-Output "Executing $TentaclePath configure --instance $instance --app ""$AppsPath --console"
    #& $TentaclePath configure --instance $instance --app ""$AppsPath --console
    #if ($LastExitCode -ne 0) {
    #    throw "Command failed"
    #}
    [xml]$xml = Get-Content $ConfigPath
    $setting = $xml.CreateElement("set")
    $setting.SetAttribute("key", "Tentacle.Deployment.ApplicationDirectory")
    $setting.InnerText = "$AppsPath"
    $xml.SelectSingleNode("./octopus-settings").AppendChild($setting)
    $xml.Save("$ConfigPath")


    #Write-Output "Executing $TentaclePath register-with --instance $instance --server $OctopusServer --environment $TentacleEnvironment --role $TentacleRole --apiKey $OctopusApiKey --comms-style TentacleActive --force --console"
    #& $TentaclePath register-with --instance $instance --server $OctopusServer --environment $TentacleEnvironment --role $TentacleRole --apiKey $OctopusApiKey --comms-style TentacleActive --force --console
    #if ($LastExitCode -ne 0) {
    #    throw "Command failed"
    #}
    #Write-Output "Executing $TentaclePath service --instance $instance --install --start --console"
    #& $TentaclePath service --instance $instance --install --start --console
    #if ($LastExitCode -ne 0) {
    #    throw "Command failed"
    #}

    exit 0
}
catch
{
    $Host.UI.WriteErrorLine($_)
    exit 1
}
