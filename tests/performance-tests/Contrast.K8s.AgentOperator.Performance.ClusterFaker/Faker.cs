// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using k8s.Models;
using KubeOps.KubernetesClient;
using Punchclock;

namespace Contrast.K8s.AgentOperator.Performance.ClusterFaker
{
    public class Faker
    {
        private const string NamespacePrefix = "contrast-faked-namespace";
        private const string DeploymentPrefix = "contrast-faked-deployment";
        private const string SecretPrefix = "contrast-faked-secret";

        private static readonly Fixture AutoFixture = new();

        private readonly IKubernetesClient _client;

        public Faker(IKubernetesClient client)
        {
            _client = client;
        }

        public async Task<int> Up(Options.UpOptions options)
        {
            Console.WriteLine($"Executing 'Up' with options '{options}'.");

            var opQueue = new OperationQueue();
            var tasks = new List<Task>();

            for (var namespaceIndex = 1; namespaceIndex <= options.NamespaceCount; namespaceIndex++)
            {
                var localIndex = namespaceIndex;
                var task = opQueue.Enqueue(localIndex * -1, () => UpNamespace(options, localIndex));
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            return 0;
        }

        private async Task UpNamespace(Options.UpOptions options, int namespaceIndex)
        {
            var namespaceName = $"{NamespacePrefix}-{namespaceIndex:D3}";
            await _client.SaveAsync(new V1Namespace
            {
                Metadata = new V1ObjectMeta(name: namespaceName)
            });

            for (var deploymentIndex = 1; deploymentIndex <= options.DeploymentsPerNamespaceCount; deploymentIndex++)
            {
                var deploymentName = $"{DeploymentPrefix}-{deploymentIndex:D3}";
                Console.WriteLine($"Creating deployment {namespaceName}/{deploymentName} "
                                  + $"(namespace {namespaceIndex}/{options.NamespaceCount}, "
                                  + $"deployment {deploymentIndex}/{options.DeploymentsPerNamespaceCount})"
                                  + "...");

                await _client.SaveAsync(new V1Deployment
                {
                    Metadata = new V1ObjectMeta(name: deploymentName, namespaceProperty: namespaceName)
                    {
                        Labels = AutoFixture.Create<Dictionary<string, string>>()
                    },
                    Spec = new V1DeploymentSpec
                    {
                        Replicas = options.PodsPerDeploymentCount,
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>
                                {
                                    { "deployment", deploymentName }
                                }
                            },
                            Spec = new V1PodSpec
                            {
                                Containers = new List<V1Container>
                                {
                                    new()
                                    {
                                        Name = $"container-{deploymentIndex:D3}",
                                        Image = "rancher/pause:3.6"
                                    }
                                }
                            }
                        },
                        Selector = new V1LabelSelector
                        {
                            MatchLabels = new Dictionary<string, string>
                            {
                                { "deployment", deploymentName }
                            }
                        }
                    }
                });
            }

            for (var secretIndex = 1; secretIndex <= options.SecretsPerNamespaceCount; secretIndex++)
            {
                var secretName = $"{SecretPrefix}-{secretIndex:D3}";
                Console.WriteLine($"Creating secret {namespaceName}/{secretName} "
                                  + $"(namespace {namespaceIndex}/{options.NamespaceCount}, "
                                  + $"secret {secretIndex}/{options.SecretsPerNamespaceCount})"
                                  + "...");

                await _client.SaveAsync(new V1Secret
                {
                    Metadata = new V1ObjectMeta(name: secretName, namespaceProperty: namespaceName)
                    {
                        Labels = AutoFixture.Create<Dictionary<string, string>>()
                    },
                    StringData = AutoFixture.Create<Dictionary<string, string>>()
                });
            }
        }

        public async Task<int> Down(Options.DownOptions options)
        {
            Console.WriteLine($"Executing 'Down' with options '{options}'.");

            var namespaces = await _client.ListAsync<V1Namespace>();
            IReadOnlyList<V1Namespace> namespacesToDelete = namespaces.Where(x => x.Name().StartsWith($"{NamespacePrefix}-"))
                                                                      .ToList();

            for (var i = 0; i < namespacesToDelete.Count; i++)
            {
                var ns = namespacesToDelete[i];
                Console.WriteLine($"Deleting '{ns.Name()}' ({i + 1}/{namespacesToDelete.Count})...");
                await _client.DeleteAsync(ns);
            }

            return 0;
        }
    }
}
