// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class TlsCertificateChainValidatorTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void When_chain_expiration_is_valid_then_IsValid_should_return_true()
        {
            var optionsFake = CreateOptions(TimeSpan.FromDays(180));
            using var chainFake = FakeCertificates(optionsFake);
            var validator = CreateValidator(optionsFake);

            // Act
            var result = validator.IsValid(chainFake, out _);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_chain_expiration_is_expired_then_IsValid_should_return_expired()
        {
            var optionsFake = CreateOptions(TimeSpan.FromDays(45));
            using var chainFake = FakeCertificates(optionsFake);
            var validator = CreateValidator(optionsFake);

            // Act
            var result = validator.IsValid(chainFake, out var reason);

            // Assert
            using (new AssertionScope())
            {
                result.Should().BeFalse();
                reason.Should().Be(ValidationResultReason.Expired);
            }
        }

        [Fact]
        public void When_chain_version_differs_then_IsValid_should_return_old_version()
        {
            var optionsFake = CreateOptions(TimeSpan.FromDays(180));
            using var chainFake = FakeCertificates(optionsFake) with
            {
                Version = AutoFixture.Create<byte[]>()
            };

            var validator = CreateValidator(optionsFake);

            // Act
            var result = validator.IsValid(chainFake, out var reason);

            // Assert
            using (new AssertionScope())
            {
                result.Should().BeFalse();
                reason.Should().Be(ValidationResultReason.OldVersion);
            }
        }

        [Fact]
        public void When_chain_sans_differs_then_IsValid_should_return_incorrect_sans()
        {
            var optionsFake = CreateOptions(TimeSpan.FromDays(180));
            using var chainFake = FakeCertificates(optionsFake) with
            {
                SanDnsNamesHash = AutoFixture.Create<byte[]>()
            };

            var validator = CreateValidator(optionsFake);

            // Act
            var result = validator.IsValid(chainFake, out var reason);

            // Assert
            using (new AssertionScope())
            {
                result.Should().BeFalse();
                reason.Should().Be(ValidationResultReason.SansIncorrect);
            }
        }

        private static TlsCertificateOptions CreateOptions(TimeSpan expiresAfter)
        {
            return AutoFixture.Create<TlsCertificateOptions>() with
            {
                ExpiresAfter = expiresAfter
            };
        }

        private static TlsCertificateChainValidator CreateValidator(TlsCertificateOptions options)
        {
            return new TlsCertificateChainValidator(options);
        }

        private static TlsCertificateChain FakeCertificates(TlsCertificateOptions options)
        {
            var generator = new TlsCertificateChainGenerator(
                new CreateCertificates(new CertificateUtility()),
                options
            );
            return generator.CreateTlsCertificateChain();
        }
    }
}
