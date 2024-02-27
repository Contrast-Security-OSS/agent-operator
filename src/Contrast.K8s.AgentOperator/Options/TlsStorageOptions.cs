// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Options;

public record TlsStorageOptions(string SecretName,
                                string SecretNamespace,
                                string ServerCertificateName = "server_certificate",
                                string CaCertificateName = "ca_certificate",
                                string CaPublicName = "ca_pem",
                                string VersionName = "compatibility_version",
                                string SanDnsNamesHashName = "sans_dns_names_hash");
