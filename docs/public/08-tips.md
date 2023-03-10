# Tips

## .NET inotify crash

By default, .NET is configured to construct inotify file watchers on any `appsettings.json` files. In Kubernetes, this is generally unneeded and can result in problems. For example, a crash during startup.

```
Unhandled exception. System.IO.IOException: The configured user limit (128) on the number of inotify instances has been reached, or the per-process limit on the number of open file descriptors has been reached.
   at System.IO.FileSystemWatcher.StartRaisingEvents()
   at Microsoft.Extensions.FileProviders.Physical.PhysicalFilesWatcher.TryEnableFileSystemWatcher()
   at Microsoft.Extensions.FileProviders.Physical.PhysicalFilesWatcher.CreateFileChangeToken(String filter)
   at Microsoft.Extensions.Primitives.ChangeToken.OnChange(Func`1 changeTokenProducer, Action changeTokenConsumer)
   at Microsoft.Extensions.Configuration.FileConfigurationProvider..ctor(FileConfigurationSource source)
   at Microsoft.Extensions.Configuration.Json.JsonConfigurationSource.Build(IConfigurationBuilder builder)
   at Microsoft.Extensions.Configuration.ConfigurationManager.AddSource(IConfigurationSource source)
   at Microsoft.Extensions.Configuration.ConfigurationManager.Microsoft.Extensions.Configuration.IConfigurationBuilder.Add(IConfigurationSource source)
   at Microsoft.Extensions.Hosting.HostingHostBuilderExtensions.ApplyDefaultAppConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder appConfigBuilder, String[] args)
   at Microsoft.Extensions.Hosting.HostApplicationBuilder..ctor(HostApplicationBuilderSettings settings)
   at Microsoft.AspNetCore.Builder.WebApplicationBuilder..ctor(WebApplicationOptions options, Action`1 configureDefaults)
   at Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(String[] args)
   at Program.<Main>$(String[] args) in /source/Program.cs:line 1
```

To prevent .NET from constructing these file watchers and to prevent the above crash, Contrast recommends to apply the environment variable:

```
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
```
