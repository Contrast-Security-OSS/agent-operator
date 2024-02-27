// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Modules;

[UsedImplicitly]
public class ChaosModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Mimics missing webhook calls.
        builder.RegisterDecorator<ChaosPodPatcher, IPodPatcher>(context =>
        {
            var options = context.Resolve<OperatorOptions>();
            return options.ChaosRatio > 0;
        });
    }

    private class ChaosPodPatcher : IPodPatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IPodPatcher _podPatcher;
        private readonly decimal _chaosRatio;

        public ChaosPodPatcher(IPodPatcher podPatcher, OperatorOptions options)
        {
            _podPatcher = podPatcher;
            _chaosRatio = options.ChaosRatio;

            Logger.Warn($"Chaos is enabled, will ignore pod patching {_chaosRatio} of the time.");
        }

        public ValueTask Patch(PatchingContext context, V1Pod pod, CancellationToken cancellationToken = default)
        {
            if ((decimal)Random.Shared.NextDouble() <= _chaosRatio)
            {
                Logger.Warn("Ignored pod patching due to chaos.");
                return ValueTask.CompletedTask;
            }

            return _podPatcher.Patch(context, pod, cancellationToken);
        }
    }
}
