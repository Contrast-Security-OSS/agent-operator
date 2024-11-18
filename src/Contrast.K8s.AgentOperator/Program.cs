// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Tls;
using k8s.Autorest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

namespace Contrast.K8s.AgentOperator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var logger = LogManager.Setup()
                               .LoadConfigurationFromAppSettings()
                               .GetCurrentClassLogger();

        try
        {
            logger.Info($"Starting the Contrast Security Agent Operator {OperatorVersion.Version}.");

            await CreateHostBuilder(args)
                         .Build()
                         .RunAsync();
        }
        catch (HttpOperationException e)
        {
            logger.Error(e, $"Fatal error during application startup. (Content: '{e.Response.Content}')");
            throw;
        }
        catch (Exception e)
        {
            logger.Error(e, "Fatal error during application startup.");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureLogging(builder =>
                   {
                       builder.ClearProviders();
                       builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                       builder.AddNLog();
                   })
                   .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                   .ConfigureWebHostDefaults(builder =>
                   {
                       builder.UseStartup<Startup>();
                       builder.UseKestrel(options =>
                       {
                           options.ConfigureHttpsDefaults(adapterOptions =>
                           {
                               var selector = options.ApplicationServices.GetRequiredService<IKestrelCertificateSelector>();
                               adapterOptions.ServerCertificateSelector += (_, s) => selector.SelectCertificate(s);
                           });
                       });
                   });
    }
}
