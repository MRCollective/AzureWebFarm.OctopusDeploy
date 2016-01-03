![AzureWebFarm.OctopusDeploy logo](https://raw.github.com/MRCollective/AzureWebFarm.OctopusDeploy/master/logo.png)

AzureWebFarm.OctopusDeploy
==========================

[![Build status](https://ci.mdavies.net/app/rest/builds/buildType:%28id:AWF_OD_CI%29,branch:%28default:true%29/statusIcon)](https://ci.mdavies.net/viewType.html?buildTypeId=AWF_OD_CI&branch_AWF_OD=%3Cdefault%3E&tab=buildTypeStatusDiv&guest=1) 
[![NuGet downloads](https://img.shields.io/nuget/dt/AzureWebFarm.OctopusDeploy.svg)](https://www.nuget.org/packages/ChameleonForms) 
[![NuGet version](https://img.shields.io/nuget/vpre/AzureWebFarm.OctopusDeploy.svg)](https://www.nuget.org/packages/ChameleonForms)

This project allows you to easily create an [infinitely-scalable farm of IIS 8 / Windows Server 2012 web servers using Windows Azure Web Roles](http://www.windowsazure.com/en-us/services/cloud-services/) that are deployed to by an [OctopusDeploy](http://octopusdeploy.com/) server.

It's really easy to get up and running - more details below, but in short:

1. Configure a standard Web Role project in Visual Studio
2. `Install-Package AzureWebFarm.OctopusDeploy`
3. Configure 5 cloud service variableos - `OctopusServer`, `OctopusApiKey`, `TentacleEnvironment`, `TentacleRole` and `TentacleMachineNameSuffix`
4. Deploy to Azure and watch the magic happen!

tl;dr
-----
* [Pre-requisites](#pre-requisites)
* [Installation Instructions](#installation-instructions)
    * [Pre-packaged deployment](#pre-packaged-deployment)
    * [Custom install](#custom-install)
* [Local debugging](#local-debugging)
* [Remote debugging](#remote-debugging)
* [What if I want to use Web Roles, but don't want to pay for another VM / don't want to use OctopusDeploy?](#what-if-i-want-to-use-web-roles-but-dont-want-to-pay-for-another-vm--dont-want-to-use-octopusdeploy)
* [What happens when I install the AzureWebFarm.OctopusDeploy NuGet package?](#what-happens-when-i-install-the-azurewebfarmoctopusdeploy-nuget-package)
* [What if I want to deploy Windows Services?](#what-if-i-want-to-deploy-windows-services)
* [What if I want to deploy non-.NET applications?](#what-if-i-want-to-deploy-non-net-applications)
* [Why is this needed?](#why-is-this-needed)
    * [If you are using OctopusDeploy for deployments and you want to move to the cloud](#if-you-are-using-octopusdeploy-for-deployments-and-you-want-to-move-to-the-cloud)
    * [If you are deploying web applications to Windows Azure](#if-you-are-deploying-web-applications-to-windows-azure)
* [Contributing](#contributing)
* [Stay abreast of the latest changes / releases](#stay-abreast-of-the-latest-changes--releases)

Logo courtesy of Aoife Doyle (thanks so much - it's awesome!)

Pre-requisites
--------------

* An [OctopusDeploy server](http://docs.octopusdeploy.com/display/OD/Getting+started) using at least version 2.1 that is already configured with the [environments](http://docs.octopusdeploy.com/display/OD/Environments), [projects](http://docs.octopusdeploy.com/display/OD/Projects), [users](http://docs.octopusdeploy.com/display/OD/Managing+users+and+teams) etc.; you will need to record the:
    * `OctopusServer` - Octopus Server URL
    * `OctopusApiKey` - [API key of a user](https://github.com/OctopusDeploy/OctopusDeploy-Api/wiki/Authentication) that has at least the following privileges in the environment you are deploying to: ("Environment manager" and "Project deployer") or ("System administrator")
    * `TentacleEnvironment` - Name of the environment that you want to deploy to
    * `TentacleRole` - Name of the role you want your web farm servers to have
    * `TentacleMachineNameSuffix` - The suffix to append to the machine name when adding the tentacle to octopus, allows for a single package to be reused with different config for multiple farms against the same OctopusDeploy server.
* Ensure that you open port 10943 on the Octopus Server so that Polling Tentacles can work, for more information view the documentation (http://docs.octopusdeploy.com/display/OD/Polling+Tentacles).
* Ensure that if you are using HTTPS (and you should be) for your OctopusDeploy server that the HTTPS certificate is [valid or you include code to trust the invalid certificate](https://github.com/OctopusDeploy/Issues/issues/742)
* You will need to [set up the website and app pool creation for your OctopusDeploy project](http://docs.octopusdeploy.com/display/OD/IIS+Websites+and+Application+Pools) (including the hostname for your site in the binding(s)) when using this library (since IIS starts off as a blank slate).
    * You can create a CName alias from your domain name to the `<mywebrolename>.cloudapp.net` address
    * If you want a naked domain then use a DNS provider that allows you to create ALIAS records or use a service like [dnsazure.com](http://dnsazure.com/)
* A [Windows Azure Cloud Service](http://www.windowsazure.com/en-us/manage/services/cloud-services/how-to-create-and-deploy-a-cloud-service/#quick) to host the web farm that has a [certificate uploaded to it](http://www.windowsazure.com/en-us/manage/services/cloud-services/how-to-create-and-deploy-a-cloud-service/#uploadcertificate) for RDP (and your HTTPS certificate if you are going to configure HTTPS)
* If you are creating a custom install (see below) then you need to have [.NET Framework 4.5 and Windows Azure Tools 2.2](http://www.microsoft.com/web/downloads/platform.aspx) installed along with Visual Studio 2012 or above
* The latest version of NuGet - at least >= 2.7.2, as there is a [bug in some earlier versions](http://docs.nuget.org/docs/release-notes/nuget-2.7.2) which can cause NuGet to miss adding a binding redirect for WindowsAzure.Storage.

Installation Instructions
-------------------------

You have two options for using AzureWebFarm.OctopusDeploy:

1. You can use one of our pre-packaged cloud packages to avoid the need to crack open Visual Studio - this is really easy, but limits you to the standard configurations we have built
2. You can install the project into a cloud project using NuGet and retain full control over how your cloud service is configured (and have the flexibility to have a non-standard configuration)

### Pre-packaged deployment
If this is something you would use (download a pre-packaged set of .cspkg and .cscfg files to upload directly to the portal rather than having your own codebase that pulls in our NuGet package) let us know so we know it's a good idea to invest time in this idea! Communicate with us via Twitter [@robdmoore](http://twitter.com/robdmoore) / [@mdaviesnet](http://twitter.com/mdaviesnet) or alternatively create an issue on this GitHub project.

### Custom install

Feel free to watch the [screencast tutorial of these instructions](http://youtu.be/2-tdTMt4dfE).

The installation instructions form two parts - normal web role installation and AzureWebFarm.OctopusDeploy installation.

**Creating a Web Role**

1. Start a new Visual Studio solution by creating a new Windows Azure Cloud Service project - be sure to select .Net Framework 4.5 and Windows Azure Tools v2.2 when creating it
    * **Don't add a web or worker role at this point - just create the blank cloud project**
    * This will henceforth be referred to as the "cloud project"
2. Add a new "ASP.NET Empty Web Application" project to your solution - ensure it's .NET Framework 4.5
    * This will henceforth be referred to as the "web project"
3. Right-click "Roles" on the cloud project, select "Add" > "Web Role Project in solution" and select the newly added web project
4. Configure RDP by right-clicking the cloud project and select "Package", tick the "Enable Remote Desktop for all roles" checkbox, select a certificate that you have uploaded to your Windows Azure cloud service and specify a username/password/expiry
5. Configure the number of instances that you want to initially deploy by changing this element in `ServiceConfiguration.Cloud.cscfg`: `<Instances count="1" />` - [use at least 2 instances to meet the 99.95% SLA](https://www.windowsazure.com/en-us/support/legal/sla/)
6. Add in a diagnostics connection string to the `ServiceConfiguration.Cloud.cscfg` file by changing this element: `<Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="AccountName={ACCOUNTNAME};AccountKey={ACCOUNTKEY};DefaultEndpointsProtocol=https" />` - make sure there is NOT a trailing `;` or it will fail when you deploy
7. Select and enter your vmsize into the `vmsize` attribute of the `WebRole` element in the `ServiceDefinition.csdef` file - I generally recommend (see the screencast for more informaton and explaination):
    * `ExtraSmall` for test/dev farms (unless your application(s) chews up all the memory)
    * `Small` for small to medium load websites
    * `Medium` for medium to high load websites
8. Consider adding a HTTPS web role endpoint and certificate (ensure the certificate is uploaded to your Cloud Service in Azure though)
9. If you want to do a test deployment at this stage to make sure everything is configured correctly first check that `osFamily` is set to `3` in `ServiceConfiguration.Cloud.cscfg` - we check this setting automatically later, but you'll need to check it yourself to successfully deploy at this stage. You may also want to check your `Web.config` and `App.config` files to make sure there is a binding redirect there for `WindowsAzure.Storage`. Deploy your cloud package to Azure - your instances should reach the "Running" state. To publish your package you can use:
    * [Visual Studio](http://msdn.microsoft.com/en-us/library/windowsazure/hh535756.aspx)
    * [The Windows Azure portal](http://www.windowsazure.com/en-us/manage/services/cloud-services/how-to-create-and-deploy-a-cloud-service/#deploy)
    * [A PowerShell script](http://www.windowsazure.com/en-us/develop/net/common-tasks/continuous-delivery/#step4) e.g. from a CI server

**Adding AzureWebFarm.OctopusDeploy to your Web Role**

1. Execute the following in the Package Manager Console (or use the GUI): `Install-Package AzureWebFarm.OctopusDeploy`
    * Make sure it installs into the web project
    * When prompted that a file has been modified click **"Reload"**
    * If prompted that a file already exists ie `WebRole.cs`, you should allow NuGet to override it with the file from our package
2. (optional) [Debug locally](#local-debugging)
3. Ensure that the `ServiceConfiguration.Cloud.cscfg` file has correct values for the `OctopusServer`, `OctopusApiKey`, `TentacleEnvironment`, `TentacleRole` and `TentacleMachineNameSuffix` variables
4. Deploy to Azure as per step 9 above

Local debugging
---------------
It is a good idea to debug the farm locally to make sure your configuration is correct and your OctopusDeploy server is configured correctly:

1. Ensure that there is only one instance locally by checking the `ServiceConfiguration.Local.cscfg` file has `<Instances count="1" />`
2. Ensure that the `ServiceConfiguration.Local.cscfg` file has correct values for the `OctopusServer`, `OctopusApiKey`, `TentacleEnvironment`, `TentacleRole` and `TentacleMachineNameSuffix` variables
3. Set the cloud project as the default project
4. (optional) If you have already downloaded the tentacle installer and don't want to wait for the emulator to download it as part of startup then place the file at `c:\Octopus.Tentacle.msi` and it will automatically be used
5. Hit F5 to start the Azure emulator
6. If all goes well you should see your computer registered with your Octopus server and any current releases deployed to your local IIS server
    * If you need to debug the startup script then uncomment the relevant REM'd out lines in Startup\startup.cmd (but remember to recomment them before dpeloying to Azure or your Azure deployment WILL fail
    * If you need to debug the RoleEntryPoint code then [set up your Visual Studio to debug using Symbol Source](http://www.symbolsource.org/Public/Home/VisualStudio) and you should be able to step into the AzureWebFarm.OctopusDeploy code

Remote debugging
----------------

The following should be able to help you debug what is happening:

* RDP into the server:
    * Look at Event Viewer for application exceptions
    * Look at C:\Resources\Temp\RoleTemp\{GUID}\StartupLog.txt to see the output of `Startup\startup.cmd`
* Look at the `LogEntity` table in table storage of the storage account you configured for Diagnostics to see the log output of the RoleEntryPoint

What if I want to use Web Roles, but don't want to pay for another VM / don't want to use OctopusDeploy?
--------------------------------------------------------------------------------------------------------

Check out our [AzureWebFarm](https://github.com/MRCollective/AzureWebFarm) project, which is a similar concept except the farm contains everything you need to deploy within it - you simply MsDeploy your application to it and it will sync the new code across the whole farm for you.

What happens when I install the AzureWebFarm.OctopusDeploy NuGet package?
-------------------------------------------------------------------------
Apart from adding the dependencies of the package and the dll the following actions are performed:

* To your web project:
    * `Startup\startup.cmd` is added and set as "Copy always"
    * `Startup\startup.ps1` is added and set as "Copy always"
    * `WebRole.cs` is added
    * Binding redirects are added to the `Web.config` file
    * The binding redirects in the `Web.config` file are copied into a new `App.config` file that is used by the RoleEntryPoint code (since it doesn't run under IIS it can't use `Web.config`)
* To your cloud project:
    * The `{CloudProject}.ccproj` file is modified to add an MSBuild target that copies the `App.config` file from the web project to `bin\{WebProject}.dll.config` in the cloud package when it's built
    * The `ServiceDefinition.csdef` file is changed to add:
        * Elevated privileges for the RoleEntryPoint code
        * Startup\startup.cmd as an elevated privileges startup task that has selected environment variables
        * A 1GB `Install` local resource directory (where the tentacle is installed) and a 19GB `Deployments` local resource directory (where deployments are stored) - if you aren't using ExtraSmall instances then you can increase the 19GB to a larger value
        * Five configuration settings variables are added: `OctopusServer`, `OctopusApiKey`, `TentacleEnvironment`, `TentacleRole` and `TentacleMachineNameSuffix`
    * All `ServiceConfiguration.*.cscfg` files are changed to add the five configuration settings variables: `OctopusServer`, `OctopusApiKey`, `TentacleEnvironment`, `TentacleRole` and `TentacleMachineNameSuffix`

To see how we perform all of this "magic" checkout the [install.ps1](https://github.com/MRCollective/AzureWebFarm.OctopusDeploy/blob/master/AzureWebFarm.OctopusDeploy/Tools/install.ps1) file.

What if I want to deploy Windows Services?
------------------------------------------
OctopusDeploy makes it [really easy to deploy Windows Services](https://octopusdeploy.com/automated-deployments/windows-service-deployment) and [we highly encourage you to perform your background processing using AzureWebFarm.OctopusDeploy](http://robdmoore.id.au/blog/2014/07/22/my-stance-on-azure-worker-roles/) in favour of Worker Roles so you can take advantage of the faster deployment time.

There is a slight caveat in that the Windows Service runs outside of the lifecycle of a role being brought up and down. This means that if your Windows Service is still running when a role restarts (e.g. due to Microsoft rolling out patches) it might still be locking dll files that Azure is trying to delete. Because of this you need to observe the following advice.

* To your service project
	* Ensure that your service startup type is set to "Manual" and not "Automatic". Otherwise your service will start before Octopus and Azure can work their magic.
	
* To your cloud project
	* Open WebRole.cs and in the OnStop() method before anything else is called add some logic to ensure the service is stopped.
	```C#
	public override void OnStop()
	{
		StopService("Your Service Name");
		_webFarmRole.OnStop();
	}

	private void StopService(string serviceName)
	{
		Log.Information("{serviceName} OnStop called", serviceName);
		try
		{
			var serviceController = new ServiceController(serviceName);
			Log.Information("{serviceName} current status is {serviceStatus}", serviceName, serviceController.Status);
			if (serviceController.Status == ServiceControllerStatus.Stopped)
			{
				Log.Warning("{serviceName} was already stopped. Nothing more to do", serviceName);
			}
			else
			{
				Log.Information("Attempting to stop {serviceName}", serviceName);
				serviceController.Stop();
				Log.Information("{serviceName} was successfully stopped", serviceName);
			}
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Error occurred while attempting to stop {serviceName}", serviceName);
			throw;
		}
	}
	```
	
What if I want to deploy non-.NET applications?
-----------------------------------------------

While .NET projects will work out of the box, IIS has the capability of running almost any programming language that you want - Python, PHP, Java, NodeJS, etc. If you are using a custom install of AzureWebFarm.OctopusDeploy then you have full flexibility to add extra startup tasks to configure IIS to enable these different languages. You can then add NuGet packages to OctopusDeploy to be deployed that don't contain a .NET application.

Why is this needed?
-------------------

### If you are using OctopusDeploy for deployments and you want to move to the cloud

If you are using OctopusDeploy for your deployments and you want to migrate your application to the cloud then this project provides you a really easy pathway to continue using OctopusDeploy for your deployments, while having the hard work of setting up the infrastructure done for you.

At the same time you benefit from the infinite scalability of Azure Web Roles (see next section for more information) and the App Initialisation Module configuration that we have enabled to improve startup performance of your applications.

### If you are deploying web applications to Windows Azure

**Why are Web Roles sometimes necessary - everyone is using Azure Web Sites these days right?**

Windows Azure Web Roles give you a [range of advantages over Windows Azure Web Sites](http://robdmoore.id.au/blog/2012/06/09/windows-azure-web-sites-vs-web-roles/) that, depending on your application, might mean you aren't able to use Web Sites. The particularly important advantages are:

* You can scale to hundreds or even thousands of nodes (note: most subscriptions start with a limit of 20 cores, but you can talk to support to get more enabled; we've seen an [example here in Australia with 500 cores for instance](http://blogs.janison.com.au/janison-blogs/2012/11/essa-in-2012.html))
* You can connect Web Roles to [Windows Azure Virtual Network](http://www.windowsazure.com/en-us/services/virtual-network/) (VPN) to enable hybrid cloud scenarios
* You can use SSL for free (it [costs extra for Web Sites](http://www.windowsazure.com/en-us/pricing/details/web-sites/#ssl-connections))
* You can open up non-standard TCP ports (i.e. anything other than 80 for HTTP and 443 for HTTPS)
* You have full control to configure IIS how you see fit
* You can run/install arbitrary software applications on role startup and if necessary with elevated privileges
* You can mount NTFS volumes from blob storage
* You can perform [complex auto-scaling](http://blogs.msdn.com/b/golive/archive/2012/04/26/auto-scaling-azure-with-wasabi-from-the-ground-up.aspx)
* You aren't restricted in what [parts of .NET you can use - e.g. GDI+ is disabled in Azure Web Sites](http://social.msdn.microsoft.com/Forums/windowsazure/en-US/b4a6eb43-0013-435f-9d11-00ee26a8d017/report-viewer-error-on-export-pdf-or-excel-from-azure-web-sites)
* You can RDP into the roles, which can sometimes make debugging easier
* You can configure complex diagnostics collection rules across the whole farm using [Windows Azure Diagnostics](http://www.windowsazure.com/en-us/develop/net/common-tasks/diagnostics/)
* You have more choice of VM size/specification and (currently at least) deployment location

If your application fits within the bounds of what's possible with [Azure Web Sites](http://www.windowsazure.com/en-us/services/web-sites/) then we generally recommend that you use it since it provides a seamless development, deployment, debugging and support experience out-of-the-box across a number of software languages and in particular the deployment experience is quick and comprehensive.

**Fair enough, so why use Web Roles over Virtual Machines - VMs are way more flexible right?**

If for one of the reasons above or perhaps one of the [other possible reasons](http://robdmoore.id.au/blog/2012/06/09/windows-azure-web-sites-vs-web-roles/) you can't use Web Sites then you are left with a choice of Windows Azure Web Roles or Windows Azure Virtual Machines. There are a number of [advantages and disadvantages to both](https://github.com/robdmoore/AzureWebAppsPresentation/), but we would generally recommend Web Roles over Virtual Machines where possible because:

* The infrastructure for Web Roles is managed for you - you automatically get Windows Updates and OS upgrades (assuming you have at least 2 roles, which is necessary for the 99.95% SLA)
* Scaling Virtual Machines is very difficult and it's impractical to scale more than 10s of nodes (without a lot of automation work and even then the storage costs would start adding up)
* Similarly, if you want to do auto-scaling beyond the basic CPU/memory scaling available in the portal then you need to implement it yourself

**OK, so what are the main disadvantages with Web Roles then - it can't be a silver bullet right?**

It must be said that Web Roles aren't perfect; there are three main disadvantages that we see with Web Roles:

1. The out-of-the-box deployment experience leaves a lot to be desired - it's slow and error-prone
    * To clarify: What is happening is *amazing* - within 8-15 minutes a number of customised, RDP-accessible, Virtual Machines are being provisioned for you on a static IP address and those machines can be scaled up or down at any time and they have health monitoring and diagnostics capabilities built-in as well as a powerful load balancer!
    * The problem lies when you tie that deployment process to the deployment of a software application (like most of the Web Roles tutorials you will read suggest you do) - waiting 8-15 minutes for a VM to be provisioned is amazing; waiting 8-15 minutes for the latest version of your software application to be deployed is unacceptably slow
    * The way we see it - you should treat a Web Role as infrastructure rather than an application and you should deploy your applications to a farm of Web Roles that have been previously set up
    * This leads us to the second disadvantage of Web Roles...
2. If you change state in a role dynamically (e.g. change IIS settings, install a program, deploy some files to IIS, etc.) then as soon as the role is restarted/recycled (e.g. a Windows Update is applied or you change a configuration setting that requires a recycle) you lose that state
    * It should be noted that this is also the main advantage of Web Roles in that it's what allows them to be so scalable - each role is treated homogenously with the others and so can be shutdown and spun up as needed - it just requires you to specify everything that is needed for that role in the package (.cspkg) file for the operation and lifetime of that role
    * The fact that you can run arbitrary code at startup and shutdown of the role means that you can do *anything you want* though; you just need to work within the bounds of the platform (it is afterall PaaS, not IaaS) - that's what we've taken advantage of to enable this project to be possible
3. Tying your application code to your Web Role:
    * Increases your solution complexity - you have another project in your solution and on top of that your deployment is more complex because rather than packaging/deploying the site you are packaging/deploying the site inside of a Web Role package
    * Often leads to developers tying the application to the Web Role's development model even though sites should be agnostic of that and leave you with flexibility to deploy it anywhere (e.g. on-premise, AWS, Azure Web Sites, etc.)

**How does AzureWebFarm.OctopusDeploy allow me to use Web Roles without any of the disadvantages?**

1. The first disadvantage above is simply taken care of by how [awesome the deployment experience is with OctopusDeploy](http://octopusdeploy.com/)
2. We've worked with the development model that Windows Azure Web Roles gives you so that the roles don't need to be changed dynamically to function as an OctopusDeploy-powered web farm; they will automatically:
    * Install an OctopusDeploy tentacle
    * Register the tentacle with an OctopusDeploy server in an environment and with a role all specified by you
    * Deploy the latest version of all relevant applications to the role before it's registered with the load balancer on startup - when it registers with the load balancer it's good-to-go
    * Configure the [IIS App Initialisation Module](http://www.iis.net/learn/get-started/whats-new-in-iis-8/iis-80-application-initialization) for all sites and app pools to improve performance of your applications
    * De-register the tentacle from the OctopusDeploy server on shutdown or recycle - thus your OctopusDeploy server will always show the current state of affairs
3. By deploying a web farm separate to your applications and then using OctopusDeploy to deploy your applications to the farm you can develop your applications completely agnostically of the fact you are deploying them to Azure

Contributing
------------

If you would like to contribute to this project then feel free to communicate with us via Twitter [@robdmoore](http://twitter.com/robdmoore) / [@mdaviesnet](http://twitter.com/mdaviesnet) or alternatively send a pull request / issue to this GitHub project.

Stay abreast of the latest changes / releases
---------------------------------------------

Follow the [MRCollective](http://twitter.com/MRCollectiveNet) twitter account.
