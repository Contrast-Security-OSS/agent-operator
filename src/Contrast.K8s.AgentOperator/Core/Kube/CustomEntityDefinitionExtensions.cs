// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using k8s;
using KubeOps.Abstractions.Entities.Attributes;
using System;
using System.Reflection;
using KubeOps.Abstractions.Entities;

namespace Contrast.K8s.AgentOperator.Core.Kube;

//Pulled from https://github.com/buehler/dotnet-kubernetes-client/blob/master/src/DotnetKubernetesClient/Entities/CustomEntityDefinitionExtensions.cs
//We no longer use the custom k8s client but these extensions are still helpful
public static class CustomEntityDefinitionExtensions
{
    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <param name="resource">The resource that is used as the type.</param>
    /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
    public static CustomEntityDefinition CreateResourceDefinition(
        this IKubernetesObject<V1ObjectMeta> resource) =>
        CreateResourceDefinition(resource.GetType());

    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
    public static CustomEntityDefinition CreateResourceDefinition<TResource>()
        where TResource : IKubernetesObject<V1ObjectMeta> =>
        CreateResourceDefinition(typeof(TResource));

    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <param name="resourceType">A type to construct the definition from.</param>
    /// <exception cref="ArgumentException">
    /// When the type of the resource does not contain a <see cref="KubernetesEntityAttribute"/>.
    /// </exception>
    /// <returns>A <see cref="CustomEntityDefinition"/>.</returns>
    public static CustomEntityDefinition CreateResourceDefinition(this Type resourceType)
    {
        var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
        }

        var scopeAttribute = resourceType.GetCustomAttribute<EntityScopeAttribute>();
        var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;

        return new CustomEntityDefinition(
            kind,
            $"{kind}List",
            attribute.Group,
            attribute.ApiVersion,
            kind.ToLower(),
            string.IsNullOrWhiteSpace(attribute.PluralName) ? $"{kind.ToLower()}s" : attribute.PluralName,
            scopeAttribute?.Scope ?? default);
    }
}

/// <summary>
/// Custom entity ("resource") definition. This is not a full CRD (custom resource definition) of
/// Kubernetes, but all parts that regard the resource. This is used to construct a CRD out of a type
/// of kubernetes entities/resources.
/// </summary>
public readonly struct CustomEntityDefinition
{
    public readonly string Kind;

    public readonly string ListKind;

    public readonly string Group;

    public readonly string Version;

    public readonly string Singular;

    public readonly string Plural;

    public readonly EntityScope Scope;

    public CustomEntityDefinition(
        string kind,
        string listKind,
        string @group,
        string version,
        string singular,
        string plural,
        EntityScope scope)
    {
        Kind = kind;
        ListKind = listKind;
        Group = @group;
        Version = version;
        Singular = singular;
        Plural = plural;
        Scope = scope;
    }
}
