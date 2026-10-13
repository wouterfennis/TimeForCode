using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
        }
    }
}