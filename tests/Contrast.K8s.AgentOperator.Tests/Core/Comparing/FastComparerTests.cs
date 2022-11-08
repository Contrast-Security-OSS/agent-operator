// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Comparing;
using FluentAssertions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Comparing
{
    public class FastComparerTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void When_compare_are_both_null_then_return_true()
        {
            string? leftFake = null;
            string? rightFake = null;

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_has_one_null_then_return_false()
        {
            string? leftFake = AutoFixture.Create<string>();
            string? rightFake = null;

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_has_same_object_then_return_true()
        {
            string? leftFake = AutoFixture.Create<string>();
            string? rightFake = leftFake;

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_are_lists_and_same_items_then_compare_items()
        {
            var leftFake = AutoFixture.CreateMany<string>().ToList();
            var rightFake = DeepClone(leftFake);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_are_lists_and_different_items_then_compare_items()
        {
            var leftFake = AutoFixture.CreateMany<string>(5).ToList();
            var rightFake = AutoFixture.CreateMany<string>(5).ToList();

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_are_lists_and_different_count_then_compare_items()
        {
            var leftFake = AutoFixture.CreateMany<string>(5).ToList();
            var rightFake = AutoFixture.CreateMany<string>(2).ToList();

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_are_lists_and_empty_then_return_true()
        {
            // ReSharper disable UseArrayEmptyMethod
            var leftFake = new string[0];
            var rightFake = new string[0];
            // ReSharper restore UseArrayEmptyMethod

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_are_dictionaries_and_empty_then_return_true()
        {
            var leftFake = new Dictionary<string, string>();
            var rightFake = DeepClone(leftFake);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_are_dictionaries_different_sizes_then_return_false()
        {
            var leftFake = AutoFixture.CreateMany<KeyValuePair<string, string>>(2).ToDictionary(x => x.Key, x => x.Value);
            var rightFake = AutoFixture.CreateMany<KeyValuePair<string, string>>(5).ToDictionary(x => x.Key, x => x.Value);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_are_dictionaries_with_different_keys_then_return_false()
        {
            var leftFake = AutoFixture.Create<Dictionary<string, string>>();
            var rightFake = AutoFixture.Create<Dictionary<string, string>>();

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_are_dictionaries_with_different_values_then_return_false()
        {
            var leftFake = new Dictionary<string, string>
            {
                { "value", AutoFixture.Create<string>() }
            };
            var rightFake = new Dictionary<string, string>
            {
                { "value", AutoFixture.Create<string>() }
            };

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_are_dictionaries_with_same_values_then_return_true()
        {
            var leftFake = AutoFixture.Create<Dictionary<string, string>>();
            var rightFake = DeepClone(leftFake);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_type_is_primitive_then_use_default_comparer()
        {
            var leftFake = AutoFixture.Create<string>();
            var rightFake = DeepClone(leftFake);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_types_are_different_return_false()
        {
            var leftFake = (Exception)new ArgumentNullException();
            var rightFake = (Exception)new ArgumentOutOfRangeException();

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void When_compare_types_are_complex_and_properties_the_same_then_return_true()
        {
            var leftFake = AutoFixture.Create<ComplexClassFixture>();
            var rightFake = DeepClone(leftFake);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void When_compare_types_are_complex_and_properties_are_different_then_return_false()
        {
            var leftFake = AutoFixture.Create<ComplexClassFixture>();
            var rightFake = AutoFixture.Create<ComplexClassFixture>();

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [return: NotNullIfNotNull("obj")]
        private static T? DeepClone<T>(T? obj)
        {
            return obj == null
                ? obj
                : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj))!;
        }

        [UsedImplicitly]
        public class ComplexClassFixture
        {
            [UsedImplicitly]
            public string? A { get; set; }

            [UsedImplicitly]
            public string? B { get; set; }

            [UsedImplicitly]
            public string? C { get; set; }
        }
    }
}
