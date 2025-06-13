// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State;

public record ResourceMetadata(string Uid, IReadOnlyCollection<MetadataAnnotations> OperatorAnnotations);
