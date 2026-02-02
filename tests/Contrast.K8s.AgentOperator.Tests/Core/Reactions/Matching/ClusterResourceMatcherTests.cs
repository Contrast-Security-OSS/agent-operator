// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Matching
{
    public class ClusterResourceMatcherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void GetMatchingBases_should_match_all_namespaces_default()
        {
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<IClusterResource>>()
                { fakeClusterResource };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBases(fakeClusterResources, namespaceName, namespaceResource);

            // Assert
            result.Should().BeEquivalentTo(fakeClusterResources);
        }

        [Fact]
        public void GetMatchingBases_should_match_namespace_name()
        {
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResourceBadName = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = new List<string> { AutoFixture.Create<string>() },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<IClusterResource>>()
                { fakeClusterResource, fakeClusterResourceBadName };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBases(fakeClusterResources, namespaceName, namespaceResource);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBases_should_match_namespace_name_glob()
        {
            var namespaceName = AutoFixture.Create<string>();
            var namespaceGlob = namespaceName.Substring(0, namespaceName.Length / 2) + "*";
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = new List<string> { namespaceGlob },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResourceBadName = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = new List<string> { AutoFixture.Create<string>() },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<IClusterResource>>()
                { fakeClusterResource, fakeClusterResourceBadName };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBases(fakeClusterResources, namespaceName, namespaceResource);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBases_should_match_namespace_label()
        {
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = labelsFake
            };

            var fakeClusterResource = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                }
            );

            var fakeClusterResource2 = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = AutoFixture.CreateMany<LabelPattern>(1).ToList()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<IClusterResource>>()
                { fakeClusterResource, fakeClusterResource2 };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBases(fakeClusterResources, namespaceName, namespaceResource);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBases_should_match_namespace_name_or_label()
        {
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = labelsFake
            };

            var fakeClusterResource = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                }
            );

            var fakeClusterResourceEmptyLabel = new ResourceIdentityPair<IClusterResource>(
                FakeNamespacedResourceIdentity<ClusterAgentConfigurationResource>(),
                AutoFixture.Create<ClusterAgentConfigurationResource>() with
                {
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<IClusterResource>>()
                { fakeClusterResource, fakeClusterResourceEmptyLabel };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBases(fakeClusterResources, namespaceName, namespaceResource);

            // Assert
            result.Should().BeEquivalentTo(fakeClusterResources);
        }


        [Fact]
        public void GetMatchingBasesForAgent_should_match_no_namespaces_default()
        {
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetMatchingBasesForAgent_should_match_namespace_name()
        {
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResourceBadName = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = new List<string> { AutoFixture.Create<string>() },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource, fakeClusterResourceBadName };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBasesForAgent_should_match_namespace_name_glob()
        {
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceGlob = namespaceName.Substring(0, namespaceName.Length / 2) + "*";
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = new List<string> { namespaceGlob },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().BeEquivalentTo(fakeClusterResources);
        }

        [Fact]
        public void GetMatchingBasesForAgent_should_not_match_correct_agent_type()
        {
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = Array.Empty<MetadataLabel>()
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResourceDotnet = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = AgentInjectionType.DotNetCore
                    },
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource, fakeClusterResourceDotnet };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBasesForAgent_should_match_namespace_label()
        {
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = labelsFake
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                }
            );

            var fakeClusterResource2 = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = AutoFixture.CreateMany<LabelPattern>(1).ToList()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource, fakeClusterResource2 };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().ContainSingle().Which.Should().BeEquivalentTo(fakeClusterResource);
        }

        [Fact]
        public void GetMatchingBasesForAgent_should_match_namespace_name_or_label()
        {
            var labelsFake = AutoFixture.CreateMany<MetadataLabel>(3).ToList();
            var agentType = AgentInjectionType.Dummy;
            var namespaceName = AutoFixture.Create<string>();
            var namespaceResource = AutoFixture.Create<NamespaceResource>() with
            {
                Labels = labelsFake
            };

            var fakeClusterResource = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = Array.Empty<string>(),
                    NamespaceLabelPatterns = labelsFake.Select(x => new LabelPattern(x.Name, x.Value)).ToList()
                }
            );

            var fakeClusterResource2 = new ResourceIdentityPair<ClusterAgentInjectorResource>(
                FakeNamespacedResourceIdentity<ClusterAgentInjectorResource>(),
                AutoFixture.Create<ClusterAgentInjectorResource>() with
                {
                    Template = AutoFixture.Create<AgentInjectorResource>() with
                    {
                        Type = agentType
                    },
                    NamespacePatterns = new List<string> { namespaceName },
                    NamespaceLabelPatterns = Array.Empty<LabelPattern>()
                }
            );

            var fakeClusterResources = new List<ResourceIdentityPair<ClusterAgentInjectorResource>>()
                { fakeClusterResource, fakeClusterResource2 };

            var matcher = CreateMatcher();

            // Act
            var result = matcher.GetMatchingBasesForAgent(fakeClusterResources, namespaceName, namespaceResource, agentType);

            // Assert
            result.Should().BeEquivalentTo(fakeClusterResources);
        }

        public ClusterResourceMatcher CreateMatcher()
        {
            return new ClusterResourceMatcher(new GlobMatcher());
        }

        public NamespacedResourceIdentity FakeNamespacedResourceIdentity<T>(string? name = null, string? namespaceName = null) where T : INamespacedResource
        {
            return NamespacedResourceIdentity.Create<T>(
                name ?? AutoFixture.Create<string>(),
                namespaceName ?? AutoFixture.Create<string>()
            );
        }

    }
}
