// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Http;
using Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions;
using FluentAssertions;
using k8s.Autorest;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Telemetry.Services.Exceptions
{
    public class TelemetryExceptionsTargetTests
    {
        [Fact]
        public void When_logger_name_is_fully_qualified_then_GetShortLoggerName_should_return_last_section()
        {
            const string input = "some.namespace.type";

            // Act
            var result = TelemetryExceptionsTarget.GetShortLoggerName(input);

            // Assert
            result.Should().Be("type");
        }

        [Fact]
        public void When_logger_name_is_already_short_then_GetShortLoggerName_should_return_as_is()
        {
            const string input = "type";

            // Act
            var result = TelemetryExceptionsTarget.GetShortLoggerName(input);

            // Assert
            result.Should().Be("type");
        }

        [Fact]
        public void When_logger_name_ends_with_separator_then_GetShortLoggerName_should_return_as_is()
        {
            const string input = "type.";

            // Act
            var result = TelemetryExceptionsTarget.GetShortLoggerName(input);

            // Assert
            result.Should().Be("type.");
        }

        [Fact]
        public void When_exception_is_not_filtered_then_ShouldReport_should_return_true()
        {
            // Act
            var result = TelemetryExceptionsTarget.ShouldReport(
                "KubeOps.Operator.Kubernetes.ResourceWatcher",
                new Exception()
            );

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_exception_is_of_not_found_filtered_then_ShouldReport_should_return_false()
        {
            // Act
            var result = TelemetryExceptionsTarget.ShouldReport(
                "KubeOps.Operator.Kubernetes.ResourceWatcher",
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(new HttpResponseMessage(HttpStatusCode.NotFound), "content")
                }
            );

            // Assert
            result.Should().BeFalse();
        }
    }
}
