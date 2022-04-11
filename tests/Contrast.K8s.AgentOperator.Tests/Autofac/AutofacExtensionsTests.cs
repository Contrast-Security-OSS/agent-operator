using Autofac;
using Contrast.K8s.AgentOperator.Autofac;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Autofac
{
    public class AutofacExtensionsTests
    {
        [Fact]
        public void METHOD_NAME()
        {
            var builder = new ContainerBuilder();

            // Act
            builder.ApplyContrastConventions(typeof(AutofacExtensionsTests).Assembly);


            // Assert
            var container = builder.Build();
            container.Resolve<ConcreteClassFixture>().Should().NotBeNull();
        }

        public class ConcreteClassFixture
        {
        }
    }
}
