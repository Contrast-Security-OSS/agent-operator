// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Extensions;
using JetBrains.Annotations;
using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
            settings.EnableLeaderElection = true;
        }).RegisterEntities();

        services.AddHealthChecks()
            .AddCheck<ReadinessCheck>(nameof(ReadinessCheck), tags: new[] { "readiness" });

        services.AddCertificateManager();
        services.AddControllers();

        // Not actually used, but hot-reload uses it?
        services.AddRazorPages();
    }

    [UsedImplicitly]
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

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health",
                new HealthCheckOptions { Predicate = reg => reg.Tags.Contains("liveness") });
            endpoints.MapHealthChecks("/ready",
                new HealthCheckOptions { Predicate = reg => reg.Tags.Contains("readiness") });
        });
    }
}
