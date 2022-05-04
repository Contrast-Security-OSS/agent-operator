using System;
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

        public ImageGenerator(ImageRepositoryOptions repositoryOptions)
        {
            _repositoryOptions = repositoryOptions;
        }

        public ValueTask<ContainerImageReference> GenerateImage(AgentInjectionType type,
                                                                string? repository,
                                                                string? name,
                                                                string? version,
                                                                CancellationToken cancellationToken = default)
        {
            repository ??= _repositoryOptions.DefaultRepository;
            name ??= type switch
            {
                AgentInjectionType.DotNetCore => "agent-operator/agents/dotnet-core",
                AgentInjectionType.Java => "agent-operator/agents/java",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            version ??= "latest";

            var reference = new ContainerImageReference(repository, name, version);
            return ValueTask.FromResult(reference);
        }
    }
}
