using Autofac;
using Contrast.K8s.AgentOperator.Autofac;
using KubeOps.Operator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Contrast.K8s.AgentOperator
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddKubernetesOperator();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.ApplyContrastConventions(typeof(Startup).Assembly);
        }

        public void Configure(IApplicationBuilder app)
        {
            // If needed:
            // var container = app.ApplicationServices.GetAutofacRoot();

            app.UseKubernetesOperator();
        }
    }
}
