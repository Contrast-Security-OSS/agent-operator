// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.State;

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
    private readonly ImageRepositoryOptions _repositoryOptions;

    public ImageGenerator(ImageRepositoryOptions repositoryOptions)
    {
        _repositoryOptions = repositoryOptions;
    }

    public ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                            string? registry,
                                                            string? name,
                                                            string? version,
                                                            CancellationToken cancellationToken = default)
    {
        registry ??= _repositoryOptions.DefaultRegistry;
        name ??= AgentInjectionTypeConverter.GetDefaultImageName(type);
        version ??= "latest";

        var reference = new ContainerImageReference(registry, name, version);
        return ValueTask.FromResult(reference);
    }
}
