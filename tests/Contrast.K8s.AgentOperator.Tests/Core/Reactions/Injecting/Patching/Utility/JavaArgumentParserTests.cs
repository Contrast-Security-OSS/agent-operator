// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Reactions.Injecting.Patching.Utility
{
    public class JavaArgumentParserTests
    {
        [Fact]
        public void Can_parse_java_example()
        {
            const string input = "-g @file1 -Dprop=value -Dws.prop=\"space spaces\" -Dwssq.prop='space spaces' -Xint";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "-g",
                "@file1",
                "-Dprop=value",
                "-Dws.prop=\"space spaces\"",
                "-Dwssq.prop='space spaces'",
                "-Xint"
            });
        }


        [Fact]
        public void Can_parse_space_separated()
        {
            const string input = "test test test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "test",
                "test"
            });
        }

        [Fact]
        public void Can_parse_quoted_words()
        {
            const string input = "test \"test\" test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "\"test\"",
                "test"
            });
        }

        [Fact]
        public void Can_parse_quoted_words_next_to_words()
        {
            const string input = "test\"test\" test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test\"test\"",
                "test"
            });
        }

        [Fact]
        public void Can_parse_quoted_words_with_spaces()
        {
            const string input = "test \"test test\"";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "\"test test\""
            });
        }

        [Fact]
        public void Can_parse_single_quotes()
        {
            const string input = "test 'test' test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "'test'",
                "test"
            });
        }

        [Fact]
        public void Can_parse_single_quote_in_double_quotes()
        {
            const string input = "test \"test' test\" test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "\"test' test\"",
                "test"
            });
        }

        [Fact]
        public void Can_parse_double_quote_in_single_quotes()
        {
            const string input = "test 'test\" test' test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "'test\" test'",
                "test"
            });
        }

        [Fact]
        public void Should_throw_unmathced_single_quote()
        {
            const string input = "test 'test test";

            // Act
            var action = () => JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            action.Should().Throw<JavaArgumentParserException>();
        }

        [Fact]
        public void Should_throw_unmatched_double_quote()
        {
            const string input = "test \"test test";

            // Act
            var action = () => JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            action.Should().Throw<JavaArgumentParserException>();
        }

        [Fact]
        public void Should_throw_following_quotes()
        {
            const string input = "test'";

            // Act
            var action = () => JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            action.Should().Throw<JavaArgumentParserException>();
        }

        [Fact]
        public void Can_parse_multiple_whitespaces()
        {
            const string input = "test  test test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test",
                "test",
                "test"
            });
        }

        [Fact]
        public void Can_parse_single_word()
        {
            const string input = "test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test"
            });
        }

        [Fact]
        public void Can_parse_leading_whitespace()
        {
            const string input = " test";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test"
            });
        }

        [Fact]
        public void Can_parse_following_whitespace()
        {
            const string input = "test ";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "test"
            });
        }

        [Fact]
        public void Can_parse_final_quotes()
        {
            const string input = "'test'";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEquivalentTo(new List<string>
            {
                "'test'"
            });
        }

        [Fact]
        public void Can_parse_only_whitespace()
        {
            const string input = " ";

            // Act
            var result = JavaArgumentParser.ParseArguments(input).ToList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Can_parse_empty()
        {
            const string input = "";

            // Act
            var result = JavaArgumentParser.ParseArguments(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Can_parse_null()
        {
            const string input = null!;

            // Act
            var result = JavaArgumentParser.ParseArguments(input);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
