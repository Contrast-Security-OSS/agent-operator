// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Matching
{
    public class AgentInjectorMatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void When_injector_matches_resource_then_GetMatchingInjectors_should_return_injector()
        {
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();

            var targetFake = new ResourceIdentityPair<IResourceWithPodTemplate>(
                FakeNamespacedResourceIdentity(),
                AutoFixture.Create<DeploymentResource>() with
                {
                    Labels = labelsFake
                }
            );

            var injectorsFake = new List<ResourceIdentityPair<AgentInjectorResource>>
            {
                new(
                    FakeNamespacedResourceIdentity(),
                    AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Selector = AutoFixture.Create<ResourceWithPodSpecSelector>() with
                        {
                            Namespaces = new List<string>
                            {
                                targetFake.Identity.Namespace
                            },
                            LabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                        }
                    }
                )
            };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingInjectors(injectorsFake, targetFake);

            // Assert
            result.Should().BeEquivalentTo(injectorsFake);
        }

        [Fact]
        public void When_matching_then_GetMatchingInjectors_should_match_on_all_labels()
        {
            // Create three labels.
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();

            var targetFake = new ResourceIdentityPair<IResourceWithPodTemplate>(
                FakeNamespacedResourceIdentity(),
                AutoFixture.Create<DeploymentResource>() with
                {
                    // Only apply one label to the resource.
                    Labels = labelsFake.Take(1).ToList()
                }
            );

            var injectorsFake = new List<ResourceIdentityPair<AgentInjectorResource>>
            {
                new(
                    FakeNamespacedResourceIdentity(),
                    AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Selector = AutoFixture.Create<ResourceWithPodSpecSelector>() with
                        {
                            Namespaces = new List<string>
                            {
                                targetFake.Identity.Namespace
                            },
                            // But match on all 3 labels.
                            LabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                        }
                    }
                )
            };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingInjectors(injectorsFake, targetFake);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IsMatch_true_when_labels_match()
        {
            var matcher = CreateMatcher();
            var result = matcher.IsMatch(
                Injector(new LabelPattern("contrast-agent", "java")),
                Workload(new MetadataLabel("contrast-agent", "java")));
            result.Should().BeTrue();
        }

        [Fact]
        public void IsMatch_false_when_label_absent()
        {
            var matcher = CreateMatcher();
            var result = matcher.IsMatch(
                Injector(new LabelPattern("contrast-agent", "java")),
                Workload(new MetadataLabel("other", "value")));
            result.Should().BeFalse();
        }

        [Fact]
        public void IsMatch_false_when_namespace_does_not_match()
        {
            var matcher = CreateMatcher();
            var result = matcher.IsMatch(
                Injector(new LabelPattern("contrast-agent", "java")),
                Workload("other-ns", new MetadataLabel("contrast-agent", "java")));
            result.Should().BeFalse();
        }

        public AgentInjectorMatcher CreateMatcher()
        {
            return new AgentInjectorMatcher(new GlobMatcher());
        }

        public NamespacedResourceIdentity FakeNamespacedResourceIdentity(string? name = null, string? @namespace = null)
        {
            return NamespacedResourceIdentity.Create<AgentInjectorResource>(
                name ?? AutoFixture.Create<string>(),
                @namespace ?? AutoFixture.Create<string>()
            );
        }

        private static ResourceIdentityPair<AgentInjectorResource> Injector(params LabelPattern[] labels)
        {
            var resource = AutoFixture.Create<AgentInjectorResource>() with
            {
                Selector = new ResourceWithPodSpecSelector(new[] { "*" }, labels, new[] { "ns" })
            };
            var identity = NamespacedResourceIdentity.Create<AgentInjectorResource>("inj", "ns");
            return new ResourceIdentityPair<AgentInjectorResource>(identity, resource);
        }

        private static ResourceIdentityPair<IResourceWithPodTemplate> Workload(params MetadataLabel[] labels)
        {
            return Workload("ns", labels);
        }

        // Overload allowing a non-default namespace on the workload's identity, to
        // exercise the selector's namespace dimension independently of labels.
        private static ResourceIdentityPair<IResourceWithPodTemplate> Workload(string @namespace, params MetadataLabel[] labels)
        {
            var deployment = AutoFixture.Create<DeploymentResource>() with { Labels = labels };
            var identity = NamespacedResourceIdentity.Create<DeploymentResource>("wl", @namespace);
            return new ResourceIdentityPair<IResourceWithPodTemplate>(identity, deployment);
        }
    }
}
