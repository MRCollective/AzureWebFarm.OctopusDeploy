AzureWebFarm.OctopusDeploy
==========================

This project allows you to easily create an [infinitely-scalable farm of IIS 8 / Windows Server 2012 web servers using Windows Azure Web Roles](http://www.windowsazure.com/en-us/services/cloud-services/) that are deployed to by an [OctopusDeploy](http://octopusdeploy.com/) server.

![AzureWebFarm.OctopusDeploy logo](https://raw.github.com/MRCollective/AzureWebFarm.OctopusDeploy/master/logo.png)
Logo courtesy of Aoife Doyle (thanks so much - it's awesome!)

tl;dr
-----
* Installation Instructions
* What if I want to use Web Roles, but don't want to pay for another VM / don't want to use OctopusDeploy?
* Why is this needed?
    * If you are using OctopusDeploy for deployments and you want to move to the cloud
    * If you are deploying web applications to Windows Azure
* Contributing
* Stay abreast of the latest changes / releases

Installation Instructions
-------------------------

You have two options for using AzureWebFarm.OctopusDeploy

1. You can use one of our pre-packaged cloud packages to avoid the need to crack open Visual Studio - this is really easy, but limits you to the standard configurations we have built
2. You can install the project into a cloud project using NuGet and retain full control over how your cloud service is configured (and have the flexibility to have a non-standard configuration)

What if I want to use Web Roles, but don't want to pay for another VM / don't want to use OctopusDeploy?
--------------------------------------------------------------------------------------------------------

Check out our [AzureWebFarm](https://github.com/MRCollective/AzureWebFarm) project, which is a similar concept except the farm contains everything you need to deploy within it - you simply MsDeploy your application to it and it will sync the new code across the whole farm for you.

Why is this needed?
-------------------

### If you are using OctopusDeploy for deployments and you want to move to the cloud

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