// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.Tls;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class TlsHelperTests
    {
        [Fact]
        public void Hashes_generated_should_be_stable()
        {
            var fake = new List<string>
            {
                "one",
                "two",
                "three"
            };

            // Act
            var result = TlsHelper.GenerateSansHash(fake.OrderBy(_ => Random.Shared.Next()));

            // Assert
            Convert.ToHexString(result).Should().Be("0C323EACC9F32E549476C7CC3E1ACE33A5DB39870BD19238921FB4CDCB44911D");
        }

        [Fact]
        public void Hashes_should_be_case_insensitive()
        {
            var fake = new List<string>
            {
                "one",
                "two",
                "THREE"
            };

            // Act
            var result = TlsHelper.GenerateSansHash(fake.OrderBy(_ => Random.Shared.Next()));

            // Assert
            Convert.ToHexString(result).Should().Be("0C323EACC9F32E549476C7CC3E1ACE33A5DB39870BD19238921FB4CDCB44911D");
        }

        [Fact]
        public void Hashes_should_ignore_duplicates()
        {
            var fake = new List<string>
            {
                "one",
                "two",
                "two",
                "three",
                "THREE"
            };

            // Act
            var result = TlsHelper.GenerateSansHash(fake.OrderBy(_ => Random.Shared.Next()));

            // Assert
            Convert.ToHexString(result).Should().Be("0C323EACC9F32E549476C7CC3E1ACE33A5DB39870BD19238921FB4CDCB44911D");
        }
    }
}
