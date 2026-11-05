using System.Net;
using System.Text.Json;
using System.Text;

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
        /// Lists every TimeForCode-owned schema, schema property, operation parameter, request body and documented response that lacks a description.
        /// A response description equal to the HTTP reason phrase (for example "Bad Request") counts as missing, because it says nothing about the endpoint; describe what the status means for the operation instead.
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
                        string label = $"{operation.Name.ToUpperInvariant()} {path.Name}";
                        CheckParameters(label, operation, gaps);
                        CheckRequestBody(label, operation, gaps);
                        CheckResponses(label, operation, gaps);
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

        private static void CheckParameters(string label, JsonProperty operation, List<string> gaps)
        {
            if (!operation.Value.TryGetProperty("parameters", out JsonElement parameters) || parameters.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            // A parameter given as a reference has no name here and is documented where it is defined.
            foreach (JsonElement parameter in parameters.EnumerateArray().Where(p => p.TryGetProperty("name", out _) && !HasDescription(p)))
            {
                gaps.Add($"Parameter '{parameter.GetProperty("name").GetString()}' of {label} has no description.");
            }
        }

        private static void CheckRequestBody(string label, JsonProperty operation, List<string> gaps)
        {
            if (operation.Value.TryGetProperty("requestBody", out JsonElement body) && !body.TryGetProperty("$ref", out _) && !HasDescription(body))
            {
                gaps.Add($"The request body of {label} has no description.");
            }
        }

        private static void CheckResponses(string label, JsonProperty operation, List<string> gaps)
        {
            if (!operation.Value.TryGetProperty("responses", out JsonElement responses))
            {
                return;
            }

            foreach (JsonProperty response in responses.EnumerateObject().Where(r => !HasSpecificResponseDescription(r)))
            {
                gaps.Add($"Response {response.Name} of {label} has no description.");
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
            StringBuilder phrase = new(name.Length + 2);

            for (int i = 0; i < name.Length; i++)
            {
                // "BadRequest" becomes "Bad Request"; "OK" has no lower-to-upper boundary and stays as it is.
                if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                {
                    phrase.Append(' ');
                }

                phrase.Append(name[i]);
            }

            return phrase.ToString();
        }

        private static bool HasDescription(JsonElement element)
        {
            return element.TryGetProperty("description", out JsonElement description)
                && description.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(description.GetString());
        }
    }
}