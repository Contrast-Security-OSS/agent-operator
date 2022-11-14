// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class TlsCertificateMaintenanceHandlerTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public async Task When_a_valid_secret_is_reconciled_then_certificate_selector_should_be_notified()
        {
            var secret = AutoFixture.Create<V1Secret>();
            using var chainFake = FakeCertificates();

            var parserMock = Substitute.For<IWebHookSecretParser>();
            parserMock.TryGetWebHookCertificateSecret(Arg.Is(secret), out _).Returns(info =>
            {
                info[1] = chainFake;
                return true;
            });
            var selector = Substitute.For<IKestrelCertificateSelector>();

            var handler = CreateGraph(selector, webHookSecretParser: parserMock);

            // Act
            await handler.Handle(new EntityReconciled<V1Secret>(secret), default);

            // Assert
            selector.Received().TakeOwnershipOfCertificate(chainFake);
        }

        [Fact]
        public async Task When_leader_elected_and_secret_is_valid_then_cluster_web_hook_should_be_updated()
        {
            var secret = AutoFixture.Create<V1Secret>();
            using var chainFake = FakeCertificates();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();

            var writerMock = Substitute.For<IKubeWebHookConfigurationWriter>();
            writerMock.FetchCurrentCertificate().Returns(secret);

            var parserMock = Substitute.For<IWebHookSecretParser>();
            parserMock.TryGetWebHookCertificateSecret(Arg.Is(secret), out _).Returns(info =>
            {
                info[1] = chainFake;
                return true;
            });

            var converterMock = Substitute.For<ITlsCertificateChainConverter>();
            converterMock.Export(chainFake).Returns(exportFake);

            var validatorMock = Substitute.For<ITlsCertificateChainValidator>();
            validatorMock.IsValid(Arg.Is(chainFake), out _).Returns(info =>
            {
                info[1] = ValidationResultReason.NoError;
                return true;
            });

            var handler = CreateGraph(
                webHookSecretParser: parserMock,
                webHookConfigurationWriter: writerMock,
                certificateChainConverter: converterMock,
                certificateChainValidator:validatorMock
            );

            // Act
            await handler.Handle(new LeaderStateChanged(true), default);

            // Assert
            await writerMock.Received().UpdateClusterWebHookConfiguration(exportFake);
        }

        [Fact]
        public async Task When_leader_elected_and_secret_is_invalid_then_new_certificate_should_be_generated()
        {
            var secret = AutoFixture.Create<V1Secret>();
            using var chainFake = FakeCertificates();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();

            var writerMock = Substitute.For<IKubeWebHookConfigurationWriter>();
            writerMock.FetchCurrentCertificate().Returns(secret);

            var parserMock = Substitute.For<IWebHookSecretParser>();
            parserMock.TryGetWebHookCertificateSecret(Arg.Is(secret), out _).Returns(info =>
            {
                info[1] = null;
                return false;
            });

            var converterMock = Substitute.For<ITlsCertificateChainConverter>();
            converterMock.Export(chainFake).Returns(exportFake);

            var generatorMock = Substitute.For<ITlsCertificateChainGenerator>();
            generatorMock.CreateTlsCertificateChain().Returns(chainFake);

            var handler = CreateGraph(
                webHookSecretParser: parserMock,
                webHookConfigurationWriter: writerMock,
                certificateChainConverter: converterMock,
                certificateChainGenerator: generatorMock
            );

            // Act
            await handler.Handle(new LeaderStateChanged(true), default);

            // Assert
            await writerMock.Received().UpdateClusterWebHookConfiguration(exportFake);
        }

        [Fact]
        public async Task When_leader_elected_and_secret_missing_then_new_certificate_should_be_generated()
        {
            using var chainFake = FakeCertificates();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();

            var writerMock = Substitute.For<IKubeWebHookConfigurationWriter>();
            writerMock.FetchCurrentCertificate().ReturnsNull();

            var converterMock = Substitute.For<ITlsCertificateChainConverter>();
            converterMock.Export(chainFake).Returns(exportFake);

            var generatorMock = Substitute.For<ITlsCertificateChainGenerator>();
            generatorMock.CreateTlsCertificateChain().Returns(chainFake);

            var handler = CreateGraph(
                webHookConfigurationWriter: writerMock,
                certificateChainConverter: converterMock,
                certificateChainGenerator: generatorMock
            );

            // Act
            await handler.Handle(new LeaderStateChanged(true), default);

            // Assert
            await writerMock.Received().UpdateClusterWebHookConfiguration(exportFake);
        }

        private static TlsCertificateMaintenanceHandler CreateGraph(IKestrelCertificateSelector? certificateSelector = null,
                                                                    ITlsCertificateChainGenerator? certificateChainGenerator = null,
                                                                    ITlsCertificateChainConverter? certificateChainConverter = null,
                                                                    IKubeWebHookConfigurationWriter? webHookConfigurationWriter = null,
                                                                    IWebHookSecretParser? webHookSecretParser = null,
                                                                    ITlsCertificateChainValidator? certificateChainValidator = null)
        {
            return new TlsCertificateMaintenanceHandler(
                certificateSelector ?? Substitute.For<IKestrelCertificateSelector>(),
                certificateChainGenerator ?? Substitute.For<ITlsCertificateChainGenerator>(),
                certificateChainConverter ?? Substitute.For<ITlsCertificateChainConverter>(),
                webHookConfigurationWriter ?? Substitute.For<IKubeWebHookConfigurationWriter>(),
                webHookSecretParser ?? Substitute.For<IWebHookSecretParser>(),
                certificateChainValidator ?? Substitute.For<ITlsCertificateChainValidator>()
            );
        }

        private static TlsCertificateChain FakeCertificates()
        {
            var generator = new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()), AutoFixture.Create<TlsCertificateOptions>());
            return generator.CreateTlsCertificateChain();
        }
    }
}
