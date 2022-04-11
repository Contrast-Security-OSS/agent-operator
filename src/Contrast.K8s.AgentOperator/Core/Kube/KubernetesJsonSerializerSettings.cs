using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public class KubernetesJsonSerializer
    {
        public static JsonSerializerSettings Settings { get; } = new()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new NamingConvention(),
            Converters = new List<JsonConverter> { new StringEnumConverter(), new Iso8601TimeSpanConverter(), },
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK"
        };

        public string SerializeObject<T>(T entity)
        {
            return JsonConvert.SerializeObject(entity, Settings);
        }

        public T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        public JToken ToJToken<T>(T entity)
        {
            return JToken.Parse(SerializeObject(entity));
        }

        internal class NamingConvention : CamelCasePropertyNamesContractResolver, INamingConvention
        {
            // https://github.com/buehler/dotnet-operator-sdk/blob/cde5ef889787f51ddba8944e28702b94d1bc4681/src/KubeOps/Operator/Serialization/NamingConvention.cs

            private readonly INamingConvention _yamlNaming = CamelCaseNamingConvention.Instance;

            private readonly IDictionary<string, string> _rename = new Dictionary<string, string>
            {
                { "namespaceProperty", "namespace" },
                { "enumProperty", "enum" },
                { "objectProperty", "object" },
                { "readOnlyProperty", "readOnly" },
                { "xKubernetesEmbeddedResource", "x-kubernetes-embedded-resource" },
                { "xKubernetesIntOrString", "x-kubernetes-int-or-string" },
                { "xKubernetesListMapKeys", "x-kubernetes-list-map-keys" },
                { "xKubernetesListType", "x-kubernetes-list-type" },
                { "xKubernetesMapType", "x-kubernetes-map-type" },
                { "xKubernetesPreserveUnknownFields", "x-kubernetes-preserve-unknown-fields" },
            };

            public string Apply(string value)
            {
                var (key, renamedValue) = _rename.FirstOrDefault(
                    p =>
                        string.Equals(value, p.Key, StringComparison.InvariantCultureIgnoreCase));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return key != default
                    ? renamedValue
                    : _yamlNaming.Apply(value);
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                var (key, renamedValue) = _rename.FirstOrDefault(
                    p =>
                        string.Equals(property.PropertyName, p.Key, StringComparison.InvariantCultureIgnoreCase));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (key != default)
                {
                    property.PropertyName = renamedValue;
                }

                return property;
            }
        }
    }
}
