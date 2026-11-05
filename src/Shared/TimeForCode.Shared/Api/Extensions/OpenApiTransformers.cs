using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace TimeForCode.Shared.Api.Extensions
{
    /// <summary>
    /// OpenAPI transformers that document behaviour the default generator does not capture.
    /// </summary>
    internal static class OpenApiTransformers
    {
        /// <summary>
        /// Documents the 429 response, with its <c>Retry-After</c> header, on every rate-limited operation.
        /// </summary>
        internal static Task AddRateLimitResponse(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var isRateLimited = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<EnableRateLimitingAttribute>()
                .Any();

            if (!isRateLimited)
            {
                return Task.CompletedTask;
            }

            operation.Responses ??= [];
            operation.Responses[StatusCodes.Status429TooManyRequests.ToString()] = new OpenApiResponse
            {
                Description = "Too many requests. Retry after the number of seconds given in the Retry-After header.",
                Headers = new Dictionary<string, IOpenApiHeader>
                {
                    ["Retry-After"] = new OpenApiHeader
                    {
                        Description = "Number of seconds to wait before retrying.",
                        Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                    }
                }
            };

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds <c>format: email</c> to properties annotated with <see cref="EmailAddressAttribute"/>.
        /// </summary>
        internal static Task AddEmailFormat(
            OpenApiSchema schema,
            OpenApiSchemaTransformerContext context,
            CancellationToken cancellationToken)
        {
            var attributes = context.JsonPropertyInfo?.AttributeProvider?
                .GetCustomAttributes(typeof(EmailAddressAttribute), inherit: true);

            if (attributes is { Length: > 0 })
            {
                schema.Format = "email";
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Documents every enum as a string schema listing its names, matching the string converter the APIs serialize with.
        /// </summary>
        internal static Task AddStringEnumValues(
            OpenApiSchema schema,
            OpenApiSchemaTransformerContext context,
            CancellationToken cancellationToken)
        {
            // Property schemas only reference the enum schema; only the schema of the type itself is rewritten.
            Type? enumType = context.JsonPropertyInfo is null
                ? Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type
                : null;

            if (enumType is not { IsEnum: true } || schema.Enum is { Count: > 0 })
            {
                return Task.CompletedTask;
            }

            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            schema.Pattern = null;
            schema.Enum = Enum.GetNames(enumType).Select(name => (JsonNode)JsonValue.Create(name)).ToList();

            return Task.CompletedTask;
        }
    }
}