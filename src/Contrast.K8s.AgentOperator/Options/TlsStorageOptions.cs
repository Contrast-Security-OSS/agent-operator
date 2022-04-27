namespace Contrast.K8s.AgentOperator.Options
{
    public record TlsStorageOptions(string SecretName,
                                    string SecretNamespace,
                                    string ServerCertificateName = "server_certificate",
                                    string CaCertificateName = "ca_certificate",
                                    string CaPublicName = "ca_pem");
}
