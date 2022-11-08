// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using Contrast.K8s.AgentOperator.Core.Comparing;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Comparing
{
    public class FastComparerHelperTests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(HttpStatusCode))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(int?))]
        public void When_type_is_primitive_then_IsPrimitive_should_return_true(Type type)
        {
            // Act
            var result = FastComparerHelper.IsPrimitive(type);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Dictionary<string, string>))]
        public void When_type_is_not_primitive_then_IsPrimitive_should_return_false(Type type)
        {
            // Act
            var result = FastComparerHelper.IsPrimitive(type);

            // Assert
            result.Should().BeFalse();
        }
    }
}
