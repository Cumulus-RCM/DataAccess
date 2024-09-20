using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace DataAccess.Shared.Tests
{
    public class ParameterValuesTests
    {
        [Fact]
        public void Serialize_ParameterValues_ToJson()
        {
            // Arrange
            var parameterValues = new ParameterValues(new List<ParameterValue>
            {
                new ParameterValue("param1", "value1", "string"),
                new ParameterValue("param2", "123", "Int32")
            });

            // Act
            var json = ParameterValues.AsJson(parameterValues);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"Name\":\"param1\"", json);
            Assert.Contains("\"Value\":\"value1\"", json);
            Assert.Contains("\"TypeName\":\"string\"", json);
            Assert.Contains("\"Name\":\"param2\"", json);
            Assert.Contains("\"Value\":\"123\"", json);
            Assert.Contains("\"TypeName\":\"Int32\"", json);
        }

        [Fact]
        public void Deserialize_Json_ToParameterValues()
        {
            // Arrange
            var json = "{\"Values\":[{\"Name\":\"param1\",\"Value\":\"value1\",\"TypeName\":\"string\"},{\"Name\":\"param2\",\"Value\":\"123\",\"TypeName\":\"Int32\"}]}";

            // Act
            var parameterValues = ParameterValues.FromJson(json);

            // Assert
            Assert.NotNull(parameterValues);
            Assert.Equal(2, parameterValues.Count);
            Assert.Equal("param1", parameterValues.Values.First().Name);
            Assert.Equal("value1", parameterValues.Values.First().Value);
            Assert.Equal("string", parameterValues.Values.First().TypeName);
            Assert.Equal("param2", parameterValues.Values.Last().Name);
            Assert.Equal("123", parameterValues.Values.Last().Value);
            Assert.Equal("Int32", parameterValues.Values.Last().TypeName);
        }

        [Fact]
        public void Serialize_And_Deserialize_ParameterValues()
        {
            // Arrange
            var originalParameterValues = new ParameterValues(new List<ParameterValue>
            {
                new ParameterValue("param1", "value1", "string"),
                new ParameterValue("param2", "123", "Int32")
            });

            // Act
            var json = ParameterValues.AsJson(originalParameterValues);
            var deserializedParameterValues = ParameterValues.FromJson(json);

            // Assert
            Assert.NotNull(deserializedParameterValues);
            Assert.Equal(originalParameterValues.Count, deserializedParameterValues.Count);
            Assert.Equal(originalParameterValues.Values.First().Name, deserializedParameterValues.Values.First().Name);
            Assert.Equal(originalParameterValues.Values.First().Value, deserializedParameterValues.Values.First().Value);
            Assert.Equal(originalParameterValues.Values.First().TypeName, deserializedParameterValues.Values.First().TypeName);
            Assert.Equal(originalParameterValues.Values.Last().Name, deserializedParameterValues.Values.Last().Name);
            Assert.Equal(originalParameterValues.Values.Last().Value, deserializedParameterValues.Values.Last().Value);
            Assert.Equal(originalParameterValues.Values.Last().TypeName, deserializedParameterValues.Values.Last().TypeName);
        }
    }
}
