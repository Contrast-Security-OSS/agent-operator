// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Monitoring
{
    public class PodTemplateStatusHandler : INotificationHandler<InjectorMatched>
    {
        private readonly IStateContainer _state;
        private readonly IResourcePatcher _patcher;

        public PodTemplateStatusHandler(IStateContainer state, IResourcePatcher patcher)
        {
            _state = state;
            _patcher = patcher;
        }

        public async Task Handle(InjectorMatched notification, CancellationToken cancellationToken)
        {
            var injector = notification.Injector;
            var target = notification.Target;

            if (await _state.GetIsDirty(target.Identity, cancellationToken))
            {
                return;
            }

            var injectionDesired = injector != null;
            var pods = await _state.GetByType<PodResource>(target.Identity.Namespace, cancellationToken);

            foreach (var (podIdentity, podResource) in GetMatchingPods(pods, target.Resource.Selector, target.Identity.Namespace))
            {
                if (await _state.GetIsDirty(podIdentity, cancellationToken))
                {
                    continue;
                }

                var desiredStatus = GetDesiredStatus(injectionDesired, podResource.IsInjected);
                if (podResource.InjectionStatus != desiredStatus)
                {
                    // If status is null, but we want InjectionRemoved, do nothing (since we can't safely remove conditionals we added).
                    if (!(podResource.InjectionStatus == null && desiredStatus.Reason == "InjectionRemoved"))
                    {
                        await _state.MarkAsDirty(podIdentity, cancellationToken);
                        await _patcher.PatchStatus<V1Pod>(
                            podIdentity.Name,
                            podIdentity.Namespace,
                            new GenericCondition
                            {
                                LastTransitionTime = DateTime.UtcNow,
                                Type = PodConditionConstants.InjectionConvergenceConditionType,
                                Message = desiredStatus.Message,
                                Reason = desiredStatus.Reason,
                                Status = desiredStatus.Status
                            }
                        );
                    }
                }
            }
        }

        private static IEnumerable<ResourceIdentityPair<PodResource>> GetMatchingPods(IEnumerable<ResourceIdentityPair<PodResource>> pods,
                                                                                      PodSelector podSelector,
                                                                                      string @namespace)
        {
            return pods.Where(x => x.Identity.Namespace.Equals(@namespace, StringComparison.OrdinalIgnoreCase)
                                   && PodMatchesSelector(x.Resource, podSelector));
        }

        private static bool PodMatchesSelector(PodResource podResource, PodSelector selector)
        {
            foreach (var (key, @operator, values) in selector.Expressions)
            {
                var value = GetFirstOrDefaultLabel(podResource, key);
                var matches = @operator switch
                {
                    LabelMatchOperation.In => values.Contains(value, StringComparer.OrdinalIgnoreCase),
                    LabelMatchOperation.NotIn => !values.Contains(value, StringComparer.OrdinalIgnoreCase),
                    LabelMatchOperation.Exists => value != null,
                    LabelMatchOperation.DoesNotExist => value == null,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!matches)
                {
                    return false;
                }
            }

            return true;
        }

        private static string? GetFirstOrDefaultLabel(PodResource podResource, string name)
        {
            // This avoids a closure allocation over a FirstOrDefault.
            // This showed up as a hot memory traffic path.
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var label in podResource.Labels)
            {
                if (string.Equals(label.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return label.Value;
                }
            }

            return null;
        }

        private static PodInjectionConvergenceCondition GetDesiredStatus(bool injectionDesired, bool isPodInjected)
        {
            if (injectionDesired)
            {
                if (isPodInjected)
                {
                    // We are in a perfect state.
                    return new PodInjectionConvergenceCondition("True", "InjectionComplete",
                        "The pod is eligible for agent injection and is currently injected.");
                }

                return new PodInjectionConvergenceCondition("False", "InjectionPending",
                    "The pod is eligible for agent injection, but is currently not injected.");
            }

            if (isPodInjected)
            {
                return new PodInjectionConvergenceCondition("False", "SuperfluousInjection",
                    "The pod is not eligible for agent injection, but is currently injected.");
            }

            // Pod not injected and not 
            return new PodInjectionConvergenceCondition("True", "InjectionRemoved",
                "The pod is not eligible for agent injection and is currently not injected.");
        }
    }
}
