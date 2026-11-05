using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using TimeForCode.Shared.Api.Extensions;

namespace TimeForCode.Shared.Tests.Api
{
    [TestClass]
    public class OpenApiTransformersTests
    {
        [TestMethod]
        public async Task AddRateLimitResponse_RateLimitedOperation_AddsTooManyRequestsResponseWithRetryAfterHeader()
        {
            var operation = new OpenApiOperation();
            var context = BuildOperationContext(new EnableRateLimitingAttribute("api"));

            await OpenApiTransformers.AddRateLimitResponse(operation, context, CancellationToken.None);

            operation.Responses.Should().ContainKey("429");
            operation.Responses!["429"].Headers.Should().ContainKey("Retry-After");
        }

        [TestMethod]
        public async Task AddRateLimitResponse_OperationWithoutRateLimiting_LeavesResponsesUntouched()
        {
            var operation = new OpenApiOperation();
            var context = BuildOperationContext();

            await OpenApiTransformers.AddRateLimitResponse(operation, context, CancellationToken.None);

            operation.Responses.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public async Task AddEmailFormat_PropertyWithEmailAddressAttribute_SetsEmailFormat()
        {
            var schema = new OpenApiSchema();
            var context = BuildSchemaContext(nameof(Sample.Email));

            await OpenApiTransformers.AddEmailFormat(schema, context, CancellationToken.None);

            schema.Format.Should().Be("email");
        }

        [TestMethod]
        public async Task AddEmailFormat_PropertyWithoutEmailAddressAttribute_LeavesFormatUntouched()
        {
            var schema = new OpenApiSchema();
            var context = BuildSchemaContext(nameof(Sample.Name));

            await OpenApiTransformers.AddEmailFormat(schema, context, CancellationToken.None);

            schema.Format.Should().BeNull();
        }

        [TestMethod]
        public async Task AddStringEnumValues_EnumSchema_DocumentsTheEnumAsItsNames()
        {
            var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
            var context = BuildTypeSchemaContext(typeof(SampleEnum));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Type.Should().Be(JsonSchemaType.String);
            schema.Format.Should().BeNull();
            schema.Enum!.Select(v => v!.GetValue<string>()).Should().Equal("First", "Second");
        }

        [TestMethod]
        public async Task AddStringEnumValues_EnumSchemaAlreadyDocumentedAsStrings_LeavesItUnchanged()
        {
            var schema = new OpenApiSchema { Enum = [JsonValue.Create("Second")!, JsonValue.Create("First")!] };
            var context = BuildTypeSchemaContext(typeof(SampleEnum));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Enum.Select(v => v!.GetValue<string>()).Should().Equal("Second", "First");
        }

        [TestMethod]
        public async Task AddStringEnumValues_NullableEnumSchema_DocumentsTheEnumAsItsNames()
        {
            var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
            var context = BuildTypeSchemaContext(typeof(SampleEnum?));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Enum!.Select(v => v!.GetValue<string>()).Should().Equal("First", "Second");
        }

        [TestMethod]
        public async Task AddStringEnumValues_NonEnumSchema_LeavesItUntouched()
        {
            var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
            var context = BuildTypeSchemaContext(typeof(int));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Type.Should().Be(JsonSchemaType.Integer);
            schema.Enum.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public async Task AddStringEnumValues_EnumPropertySchema_DocumentsTheEnumAsItsNames()
        {
            var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Description = "The state." };
            var context = BuildEnumPropertySchemaContext(nameof(Sample.State));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Type.Should().Be(JsonSchemaType.String);
            schema.Enum!.Select(v => v!.GetValue<string>()).Should().Equal("First", "Second");
            schema.Description.Should().Be("The state.");
        }

        [TestMethod]
        public async Task AddStringEnumValues_NullableEnumPropertySchema_KeepsItNullable()
        {
            var schema = new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null };
            var context = BuildEnumPropertySchemaContext(nameof(Sample.OptionalState));

            await OpenApiTransformers.AddStringEnumValues(schema, context, CancellationToken.None);

            schema.Type.Should().Be(JsonSchemaType.String | JsonSchemaType.Null);
        }

        private static OpenApiSchemaTransformerContext BuildEnumPropertySchemaContext(string propertyName)
        {
            var propertyInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(Sample)).Properties
                .Single(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            return new OpenApiSchemaTransformerContext
            {
                DocumentName = "v1",
                JsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(propertyInfo.PropertyType),
                JsonPropertyInfo = propertyInfo,
                ParameterDescription = null,
                ApplicationServices = null!
            };
        }

        private static OpenApiSchemaTransformerContext BuildTypeSchemaContext(Type type)
        {
            return new OpenApiSchemaTransformerContext
            {
                DocumentName = "v1",
                JsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(type),
                JsonPropertyInfo = null,
                ParameterDescription = null,
                ApplicationServices = null!
            };
        }

        private enum SampleEnum
        {
            First,
            Second
        }

        private static OpenApiOperationTransformerContext BuildOperationContext(params object[] endpointMetadata)
        {
            return new OpenApiOperationTransformerContext
            {
                DocumentName = "v1",
                Description = new ApiDescription
                {
                    ActionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata }
                },
                ApplicationServices = null!
            };
        }

        private static OpenApiSchemaTransformerContext BuildSchemaContext(string propertyName)
        {
            var typeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(Sample));
            var propertyInfo = typeInfo.Properties.Single(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            return new OpenApiSchemaTransformerContext
            {
                DocumentName = "v1",
                JsonTypeInfo = propertyInfo.PropertyType == typeof(string) ? JsonSerializerOptions.Default.GetTypeInfo(typeof(string)) : typeInfo,
                JsonPropertyInfo = propertyInfo,
                ParameterDescription = null,
                ApplicationServices = null!
            };
        }

        private sealed class Sample
        {
            [EmailAddress]
            public string? Email { get; init; }

            public string? Name { get; init; }

            public SampleEnum State { get; init; }

            public SampleEnum? OptionalState { get; init; }
        }
    }
}