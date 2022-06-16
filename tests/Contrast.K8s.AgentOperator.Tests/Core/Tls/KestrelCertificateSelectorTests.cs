// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class KestrelCertificateSelectorTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void When_no_certificate_matches_then_SelectCertificate_should_return_null()
        {
            IKestrelCertificateSelector selector = new KestrelCertificateSelector();

            // Act
            using var result = selector.SelectCertificate(AutoFixture.Create<string>());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void When_taking_ownership_is_successful_then_TakeOwnershipOfCertificate_should_return_success()
        {
            IKestrelCertificateSelector selector = new KestrelCertificateSelector();

            using var chainFake = FakeCertificates();

            // Act
            var result = selector.TakeOwnershipOfCertificate(chainFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_taking_ownership_is_successful_then_SelectCertificate_should_return_new_certificate()
        {
            IKestrelCertificateSelector selector = new KestrelCertificateSelector();

            using var chainFake = FakeCertificates();

            // Act
            selector.TakeOwnershipOfCertificate(chainFake);
            using var result = selector.SelectCertificate(AutoFixture.Create<string>());

            // Assert
            result.Should().Be(chainFake.ServerCertificate);
        }

        private static TlsCertificateChain FakeCertificates()
        {
            var generator = new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()), AutoFixture.Create<TlsCertificateOptions>());
            return generator.CreateTlsCertificateChain();
        }
    }
}
