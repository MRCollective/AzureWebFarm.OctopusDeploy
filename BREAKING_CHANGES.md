AzureWebFarm.OctopusDeploy Breaking Changes
===========================================

Version 2.0.0
-------------

### OctopusDeploy 3 Support

We now use the API client library from Octopus Deploy 3 - it's likely that this would break Octopus Deploy 2 so use with caution ifi you are still on 2 or use the 1.x series of AzureWebFarm.OctopusDeploy.

### `WebFarmRole.Run()` signature changed

WebFarm role now takes a `CancellationToken` and returns a `Task` so it can be awaited. This makes it easier to support other code running side-by-side in your role and exert more control over the `Run` method.

If you want the behaviour to stay the same as before then put the following in your `WebRole` (RoleEntryPoint) class `Run` method:

```c#
        public override void Run()
        {
            Task.Run(() => _webFarmRole.Run(new CancellationTokenSource().Token)).Wait();
        }
```

Version 1.2.0
-------------

### `TentacleMachineNameSuffix` cloud config property added

You need to make sure that your `.cscfg` and `.csdef` files have the new `TentacleMachineNameSuffix` config property. It is allowed to be blank. When you update the NuGet package it should automatically add it in for you.

This property was added to allow multiple farms to be deployed to the same OctopusDeploy server from the same Visual Studio solution.
