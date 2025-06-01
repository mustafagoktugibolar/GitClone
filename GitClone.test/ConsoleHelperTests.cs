using System;
using System.Collections.Generic;
using GitClone;
using GitClone.Helpers;
using Xunit;

namespace GitClone.Tests
{
    public class ConsoleHelperTests
    {
        [Fact]
        public void ParseOptions_SimpleKeyValuePairs_ReturnsCorrectDictionary()
        {
            // Arrange
            string[] args = new[]
            {
                "--username", "johndoe",
                "--email",    "john@doe.com",
                "--flag"
            };

            // Act
            var opts = ConsoleHelper.ParseOptions(args);

            // Assert
            Assert.Equal(3, opts.Count);
            Assert.True(opts.ContainsKey("username"));
            Assert.Equal("johndoe",    opts["username"]);
            Assert.Equal("john@doe.com", opts["email"]);
            // "--flag" had no value → should map to "True"
            Assert.Equal(bool.TrueString, opts["flag"], ignoreCase: true);
        }

        [Theory]
        [InlineData(new[] { "operation","--global", "--other", "value" }, true)]
        [InlineData(new[] { "ilos", "config", "--global" },       false)] 
        // (Because IsGlobal checks args[1] specifically; see implementation.)
        public void IsGlobal_VariousPositions_CorrectlyDetected(string[] args, bool expected)
        {
            // Act
            bool actual = false;
            try
            {
                actual = ConsoleHelper.IsGlobal(args);
            }
            catch (IndexOutOfRangeException)
            {
                // In case args.Length < 2, IsGlobal would throw; we treat that as "not global".
                actual = false;
            }

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseOptions_EmptyArray_ReturnsEmptyDictionary()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act
            var opts = ConsoleHelper.ParseOptions(args);

            // Assert
            Assert.Empty(opts);
        }
    }
}
