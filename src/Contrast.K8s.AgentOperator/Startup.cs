using Autofac;
using Contrast.K8s.AgentOperator.Autofac;
using DotnetKubernetesClient;
using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddKubernetesOperator(settings =>
            {
                if (_environment.IsDevelopment())
                {
                    settings.EnableLeaderElection = false;
                }
            });
        }

        // ReSharper disable once UnusedMember.Global
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.ApplyContrastConventions(typeof(Startup).Assembly);

            builder.Register(_ => new KubernetesClient()).As<IKubernetesClient>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // If needed:
            // var container = app.ApplicationServices.GetAutofacRoot();

            if (_environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseKubernetesOperator();
        }
    }
}
