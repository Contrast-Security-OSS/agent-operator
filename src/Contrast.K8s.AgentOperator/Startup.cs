// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Contrast.K8s.AgentOperator.Autofac;
using Contrast.K8s.AgentOperator.Core;
using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator;

public class Startup
{
    private readonly IWebHostEnvironment _environment;

    public Startup(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator(settings =>
                {
                    // We handle leadership ourselves.
                    settings.OnlyWatchEventsWhenLeader = false;

                    // This controls how often we perform a full state re-sync.
                    // So best to keep this high, especially for larger clusters.
                    settings.WatcherHttpTimeout = 60 * 10;
                })
                .AddReadinessCheck<ReadinessCheck>();
        services.AddCertificateManager();
        services.AddControllers();

        // Not actually used, but hot-reload uses it?
        services.AddRazorPages();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        var assembly = typeof(Startup).Assembly;

        builder.ApplyContrastConventions(assembly);
        builder.RegisterAssemblyModules(assembly);
    }

    public void Configure(IApplicationBuilder app)
    {
        if (_environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseKubernetesOperator();

        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
