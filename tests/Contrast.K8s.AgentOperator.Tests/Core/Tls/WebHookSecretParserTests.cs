// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class WebHookSecretParserTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void When_secret_is_invalid_then_TryGetWebHookCertificateSecret_should_return_false()
        {
            var secretFake = AutoFixture.Create<V1Secret>();
            var parser = CreateGraph();

            // Act
            var result = parser.TryGetWebHookCertificateSecret(secretFake, out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_secret_is_valid_then_TryGetWebHookCertificateSecret_should_return_true()
        {
            var optionsFake = AutoFixture.Create<TlsStorageOptions>();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();
            using var chainFake = FakeCertificates();
            var secretFake = new V1Secret
            {
                Metadata = new V1ObjectMeta(name: optionsFake.SecretName, namespaceProperty: optionsFake.SecretNamespace),
                Data = new Dictionary<string, byte[]>
                {
                    { optionsFake.ServerCertificateName, exportFake.ServerCertificatePfx },
                    { optionsFake.CaPublicName, exportFake.CaPublicPem },
                    { optionsFake.CaCertificateName, exportFake.CaCertificatePfx },
                    { optionsFake.VersionName, exportFake.Version },
                    { optionsFake.SanDnsNamesHashName, exportFake.SansHash },
                }
            };

            var converterMock = Substitute.For<ITlsCertificateChainConverter>();
            converterMock.Import(Arg.Is(exportFake)).Returns(chainFake);

            var parser = CreateGraph(optionsFake, converterMock);

            // Act
            var success = parser.TryGetWebHookCertificateSecret(secretFake, out var result);

            // Assert
            using (new AssertionScope())
            {
                success.Should().BeTrue();
                result.Should().Be(chainFake);
            }
        }

        [Fact]
        public void When_secret_is_valid_but_expired_then_TryGetWebHookCertificateSecret_should_return_false()
        {
            var optionsFake = AutoFixture.Create<TlsStorageOptions>();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();
            using var chainFake = FakeCertificates(TimeSpan.FromDays(14));
            var secretFake = new V1Secret
            {
                Metadata = new V1ObjectMeta(name: optionsFake.SecretName, namespaceProperty: optionsFake.SecretNamespace),
                Data = new Dictionary<string, byte[]>
                {
                    { optionsFake.ServerCertificateName, exportFake.ServerCertificatePfx },
                    { optionsFake.CaPublicName, exportFake.CaPublicPem },
                    { optionsFake.CaCertificateName, exportFake.CaCertificatePfx },
                    { optionsFake.VersionName, exportFake.Version },
                }
            };

            var converterMock = Substitute.For<ITlsCertificateChainConverter>();
            converterMock.Import(Arg.Is(exportFake)).Returns(chainFake);

            var parser = CreateGraph(optionsFake, converterMock);

            // Act
            var result = parser.TryGetWebHookCertificateSecret(secretFake, out _);

            // Assert
            result.Should().BeFalse();
        }

        private static IWebHookSecretParser CreateGraph(TlsStorageOptions? tlsStorageOptions = null,
                                                        ITlsCertificateChainConverter? certificateChainConverter = null)
        {
            return new WebHookSecretParser(
                tlsStorageOptions ?? AutoFixture.Create<TlsStorageOptions>(),
                certificateChainConverter ?? Substitute.For<ITlsCertificateChainConverter>()
            );
        }

        private static TlsCertificateChain FakeCertificates(TimeSpan? expiresIn = null)
        {
            var options = AutoFixture.Build<TlsCertificateOptions>()
                                     .With(x => x.ExpiresAfter, expiresIn ?? TimeSpan.FromDays(180))
                                     .Create();
            var generator = new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()), options);
            return generator.CreateTlsCertificateChain();
        }
    }
}
