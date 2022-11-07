// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Performance.ClusterFaker
{
    public class Faker
    {
        private const string NamespacePrefix = "contrast-faked-namespace";
        private const string DeploymentPrefix = "contrast-faked-deployment";

        private readonly IKubernetesClient _client;

        public Faker(IKubernetesClient client)
        {
            _client = client;
        }

        public async Task<int> Up(Options.UpOptions options)
        {
            Console.WriteLine($"Executing 'Up' with options '{options}'.");

            for (var namespaceIndex = 1; namespaceIndex <= options.NamespaceCount; namespaceIndex++)
            {
                var namespaceName = $"{NamespacePrefix}-{namespaceIndex:D3}";
                await _client.Save(new V1Namespace
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

                    await _client.Save(new V1Deployment
                    {
                        Metadata = new V1ObjectMeta(name: deploymentName, namespaceProperty: namespaceName),
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
            }

            return 0;
        }

        public async Task<int> Down(Options.DownOptions options)
        {
            Console.WriteLine($"Executing 'Down' with options '{options}'.");

            var namespaces = await _client.List<V1Namespace>();
            IReadOnlyList<V1Namespace> namespacesToDelete = namespaces.Where(x => x.Name().StartsWith($"{NamespacePrefix}-"))
                                                                      .ToList();

            for (var i = 0; i < namespacesToDelete.Count; i++)
            {
                var ns = namespacesToDelete[i];
                Console.WriteLine($"Deleting '{ns.Name()}' ({i + 1}/{namespacesToDelete.Count})...");
                await _client.Delete(ns);
            }

            return 0;
        }
    }
}
