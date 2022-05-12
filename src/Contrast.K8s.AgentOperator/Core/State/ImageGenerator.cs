using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Options;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IImageGenerator
    {
        ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                         string? repository,
                                                         string? name,
                                                         string? version,
                                                         CancellationToken cancellationToken = default);
    }

    public class ImageGenerator : IImageGenerator
    {
        private readonly ImageRepositoryOptions _repositoryOptions;
        private readonly IAgentInjectionTypeConverter _injectionTypeConverter;

        public ImageGenerator(ImageRepositoryOptions repositoryOptions,
                              IAgentInjectionTypeConverter injectionTypeConverter)
        {
            _repositoryOptions = repositoryOptions;
            _injectionTypeConverter = injectionTypeConverter;
        }

        public ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                                string? repository,
                                                                string? name,
                                                                string? version,
                                                                CancellationToken cancellationToken = default)
        {
            repository ??= _repositoryOptions.DefaultRepository;
            name ??= _injectionTypeConverter.GetDefaultImageName(type);
            version ??= "latest";

            var reference = new ContainerImageReference(repository, name, version);
            return ValueTask.FromResult(reference);
        }
    }
}
