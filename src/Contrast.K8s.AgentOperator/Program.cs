using System;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Contrast.K8s.AgentOperator.Core.Tls;
using KubeOps.Operator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

namespace Contrast.K8s.AgentOperator
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var logger = LogManager.Setup()
                                   .LoadConfigurationFromAppSettings()
                                   .GetCurrentClassLogger();

            try
            {
                return await CreateHostBuilder(args)
                             .Build()
                             .RunOperatorAsync(args);
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
}
