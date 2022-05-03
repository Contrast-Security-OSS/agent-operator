using System;
using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class TlsCertificateChainGeneratorTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void Ca_certificate_should_be_valid()
        {
            var options = AutoFixture.Build<TlsCertificateOptions>()
                                     .With(x => x.ExpiresAfter, TimeSpan.FromDays(180))
                                     .Create();
            var generator = CreateGraph(options);

            // Act
            using var result = generator.CreateTlsCertificateChain();

            // Assert
            using (new AssertionScope())
            {
                var caCertificate = result.CaCertificate;

                caCertificate.PublicKey.GetRSAPublicKey()?.KeySize.Should().Be(2048);

                caCertificate.NotBefore.Should().BeBefore(DateTime.Now);
                caCertificate.NotAfter.Should().BeAfter(DateTime.Now);
            }
        }

        [Fact]
        public void Server_certificate_should_be_valid()
        {
            var options = AutoFixture.Build<TlsCertificateOptions>()
                                     .With(x => x.ExpiresAfter, TimeSpan.FromDays(180))
                                     .Create();
            var generator = CreateGraph(options);

            // Act
            using var result = generator.CreateTlsCertificateChain();

            // Assert
            using (new AssertionScope())
            {
                var serverCertificate = result.ServerCertificate;

                serverCertificate.PublicKey.GetRSAPublicKey()?.KeySize.Should().Be(2048);

                serverCertificate.NotBefore.Should().BeBefore(DateTime.Now);
                serverCertificate.NotAfter.Should().BeAfter(DateTime.Now);
            }
        }

        private static TlsCertificateChainGenerator CreateGraph(TlsCertificateOptions options)
        {
            return new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()), options);
        }
    }
}
