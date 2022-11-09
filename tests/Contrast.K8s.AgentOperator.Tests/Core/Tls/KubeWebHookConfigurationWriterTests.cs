// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class KubeWebHookConfigurationWriterTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public async Task When_fetching_secrets_the_configured_options_should_be_used()
        {
            var tlsStorageOptionsFake = AutoFixture.Create<TlsStorageOptions>();
            var secretFake = AutoFixture.Create<V1Secret>();

            var kubernetesClientMock = Substitute.For<IKubernetesClient>();
            kubernetesClientMock.Get<V1Secret>(tlsStorageOptionsFake.SecretName, tlsStorageOptionsFake.SecretNamespace).Returns(secretFake);

            var writer = CreateGraph(tlsStorageOptionsFake, kubernetesClient: kubernetesClientMock);

            // Act
            var result = await writer.FetchCurrentCertificate();

            // Assert
            result.Should().Be(secretFake);
        }

        [Fact]
        public async Task When_updating_cluster_certificate_then_secret_should_be_valid()
        {
            var storageOptionsFake = AutoFixture.Create<TlsStorageOptions>();
            var exportChainFake = AutoFixture.Create<TlsCertificateChainExport>();

            var kubernetesClientMock = Substitute.For<IKubernetesClient>();
            V1Secret? savedSecret = null;
            kubernetesClientMock.When(x => x.Save(Arg.Any<V1Secret>())).Do(info => savedSecret = info.Arg<V1Secret>());

            var writer = CreateGraph(storageOptionsFake, kubernetesClient: kubernetesClientMock);

            // Act
            await writer.UpdateClusterWebHookConfiguration(exportChainFake);

            // Assert
            using (new AssertionScope())
            {
                savedSecret.Should().NotBeNull();
                savedSecret.Name().Should().Be(storageOptionsFake.SecretName);
                savedSecret.Namespace().Should().Be(storageOptionsFake.SecretNamespace);
                savedSecret!.Data.Should().ContainKey(storageOptionsFake.CaCertificateName)
                            .WhoseValue.Should().BeEquivalentTo(exportChainFake.CaCertificatePfx);
                savedSecret!.Data.Should().ContainKey(storageOptionsFake.CaPublicName)
                            .WhoseValue.Should().BeEquivalentTo(exportChainFake.CaPublicPem);
                savedSecret!.Data.Should().ContainKey(storageOptionsFake.ServerCertificateName)
                            .WhoseValue.Should().BeEquivalentTo(exportChainFake.ServerCertificatePfx);
                savedSecret!.Data.Should().ContainKey(storageOptionsFake.VersionName)
                            .WhoseValue.Should().BeEquivalentTo(exportChainFake.Version);
            }
        }

        [Fact]
        public async Task When_updating_webhook_then_web_hook_should_have_correct_ca_bundle()
        {
            var webHookOptionsFake = AutoFixture.Create<MutatingWebHookOptions>();
            var webHookFactory = AutoFixture.Build<V1MutatingWebhook>()
                                            .With(x => x.Name, webHookOptionsFake.WebHookName);
            var webHookConfigurationFake = AutoFixture.Build<V1MutatingWebhookConfiguration>()
                                                      .With(x => x.Webhooks, webHookFactory.CreateMany(1).ToList)
                                                      .Create();
            var exportFake = AutoFixture.Create<TlsCertificateChainExport>();

            var patcherMock = Substitute.For<IResourcePatcher>();
            patcherMock.When(x => x.Patch(webHookOptionsFake.ConfigurationName, null, Arg.Any<Action<V1MutatingWebhookConfiguration>>()))
                       .Do(info => { info.Arg<Action<V1MutatingWebhookConfiguration>>().Invoke(webHookConfigurationFake); });

            var writer = CreateGraph(mutatingWebHookOptions: webHookOptionsFake, resourcePatcher: patcherMock);

            // Act
            await writer.UpdateClusterWebHookConfiguration(exportFake);

            // Assert
            webHookConfigurationFake.Webhooks.First().ClientConfig.CaBundle.Should().BeEquivalentTo(exportFake.CaPublicPem);
        }

        private static IKubeWebHookConfigurationWriter CreateGraph(TlsStorageOptions? tlsStorageOptions = null,
                                                                   MutatingWebHookOptions? mutatingWebHookOptions = null,
                                                                   IKubernetesClient? kubernetesClient = null,
                                                                   IResourcePatcher? resourcePatcher = null)
        {
            return new KubeWebHookConfigurationWriter(
                tlsStorageOptions ?? AutoFixture.Create<TlsStorageOptions>(),
                mutatingWebHookOptions ?? AutoFixture.Create<MutatingWebHookOptions>(),
                kubernetesClient ?? Substitute.For<IKubernetesClient>(),
                resourcePatcher ?? Substitute.For<IResourcePatcher>()
            );
        }
    }
}
