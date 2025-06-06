﻿// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Options;

public record InitContainerOptions(
    string CpuRequest,
    string CpuLimit,
    string MemoryRequest,
    string MemoryLimit,
    string EphemeralStorageRequest,
    string EphemeralStorageLimit);
