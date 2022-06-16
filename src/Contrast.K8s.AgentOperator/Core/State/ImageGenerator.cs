// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IImageGenerator
    {
        ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                         string? registry,
                                                         string? name,
                                                         string? version,
                                                         CancellationToken cancellationToken = default);
    }

    public class ImageGenerator : IImageGenerator
    {
        private readonly IAgentInjectionTypeConverter _injectionTypeConverter;

        public ImageGenerator(IAgentInjectionTypeConverter injectionTypeConverter)
        {
            _injectionTypeConverter = injectionTypeConverter;
        }

        public ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                                string? registry,
                                                                string? name,
                                                                string? version,
                                                                CancellationToken cancellationToken = default)
        {
            registry ??= _injectionTypeConverter.GetDefaultImageRegistry(type);
            name ??= _injectionTypeConverter.GetDefaultImageName(type);
            version ??= "latest";

            var reference = new ContainerImageReference(registry, name, version);
            return ValueTask.FromResult(reference);
        }
    }
}
