AzureWebFarm.OctopusDeploy Breaking Changes
-------------------------------------------

Version 1.2.0
=============

### `TentacleMachineNameSuffix` cloud config property added

You need to make sure that your `.cscfg` and `.csdef` files have the new `TentacleMachineNameSuffix` config property. It is allowed to be blank. When you update the NuGet package it should automatically add it in for you.

This property was added to allow multiple farms to be deployed to the same OctopusDeploy server from the same Visual Studio solution.
