using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TimeForCode.Shared.ApiDocs
{
    /// <summary>
    /// Finds documentation gaps in a generated OpenAPI document.
    /// </summary>
    public static class OpenApiDocumentationCheck
    {
        // Framework-owned schemas that TimeForCode cannot document.
        private static readonly string[] ExemptSchemas = ["ProblemDetails", "HttpValidationProblemDetails"];

        /// <summary>
        /// Lists every TimeForCode-owned schema, schema property and documented response that lacks a description.
        /// </summary>
        /// <param name="openApiJson">The OpenAPI document as JSON.</param>
        /// <returns>One line per gap; empty when the document is complete.</returns>
        public static IReadOnlyList<string> FindGaps(string openApiJson)
        {
            using JsonDocument document = JsonDocument.Parse(openApiJson);
            List<string> gaps = [];

            if (document.RootElement.TryGetProperty("components", out JsonElement components)
                && components.TryGetProperty("schemas", out JsonElement schemas))
            {
                foreach (JsonProperty schema in schemas.EnumerateObject().Where(s => !ExemptSchemas.Contains(s.Name)))
                {
                    CheckSchema(schema, gaps);
                }
            }

            if (document.RootElement.TryGetProperty("paths", out JsonElement paths))
            {
                foreach (JsonProperty path in paths.EnumerateObject())
                {
                    foreach (JsonProperty operation in path.Value.EnumerateObject().Where(o => o.Value.ValueKind == JsonValueKind.Object))
                    {
                        CheckResponses(path.Name, operation, gaps);
                    }
                }
            }

            return gaps;
        }

        private static void CheckSchema(JsonProperty schema, List<string> gaps)
        {
            if (!HasDescription(schema.Value))
            {
                gaps.Add($"Schema '{schema.Name}' has no description.");
            }

            if (schema.Value.TryGetProperty("properties", out JsonElement properties))
            {
                foreach (JsonProperty property in properties.EnumerateObject().Where(p => !HasDescription(p.Value)))
                {
                    gaps.Add($"Property '{property.Name}' of schema '{schema.Name}' has no description.");
                }
            }
        }

        private static void CheckResponses(string path, JsonProperty operation, List<string> gaps)
        {
            if (!operation.Value.TryGetProperty("responses", out JsonElement responses))
            {
                return;
            }

            foreach (JsonProperty response in responses.EnumerateObject().Where(r => !HasSpecificResponseDescription(r)))
            {
                gaps.Add($"Response {response.Name} of {operation.Name.ToUpperInvariant()} {path} has no description.");
            }
        }

        // The generator fills in the HTTP reason phrase ("Bad Request") when a response is not documented, which says nothing about the endpoint.
        private static bool HasSpecificResponseDescription(JsonProperty response)
        {
            if (!HasDescription(response.Value))
            {
                return false;
            }

            string description = response.Value.GetProperty("description").GetString()!.Trim();
            return !(int.TryParse(response.Name, out int status) && description == ReasonPhrase(status));
        }

        private static string ReasonPhrase(int status)
        {
            string name = ((HttpStatusCode)status).ToString();
            return name == "OK" ? name : Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }

        private static bool HasDescription(JsonElement element)
        {
            return element.TryGetProperty("description", out JsonElement description)
                && description.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(description.GetString());
        }
    }
}