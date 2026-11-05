using AwesomeAssertions;
using TimeForCode.Shared.ApiDocs;

namespace TimeForCode.Shared.Tests.ApiDocs
{
    [TestClass]
    public class OpenApiDocumentationCheckTests
    {
        private const string DocumentedDocument = """
            {
              "paths": { "/a": { "get": { "parameters": [ { "name": "id", "in": "path", "description": "The identifier." } ], "responses": {
                "200": { "description": "The report." },
                "400": { "description": "The identifier is malformed." } } } },
                "/b": { "post": { "requestBody": { "description": "The payload." }, "responses": { "204": { "description": "Stored." } } } } },
              "components": { "schemas": {
                "Report": { "description": "A report.", "properties": { "hours": { "description": "The hours." } } },
                "ProblemDetails": { "properties": { "title": { "type": "string" } } } } }
            }
            """;

        [TestMethod]
        public void FindGaps_FullyDocumentedDocument_ReturnsNoGaps()
        {
            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(DocumentedDocument);

            // Assert
            gaps.Should().BeEmpty();
        }

        [TestMethod]
        public void FindGaps_SchemaWithoutDescription_ReportsTheSchema()
        {
            // Arrange
            string document = DocumentedDocument.Replace("\"description\": \"A report.\", ", string.Empty);

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("Schema 'Report'");
        }

        [TestMethod]
        public void FindGaps_PropertyWithoutDescription_ReportsTheProperty()
        {
            // Arrange
            string document = DocumentedDocument.Replace("{ \"description\": \"The hours.\" }", "{ \"type\": \"integer\" }");

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("Report").And.Contain("hours");
        }

        [TestMethod]
        public void FindGaps_ResponseWithoutDescription_ReportsTheOperationAndStatus()
        {
            // Arrange
            string document = DocumentedDocument.Replace("{ \"description\": \"The identifier is malformed.\" }", "{ }");

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("GET /a").And.Contain("400");
        }

        [TestMethod]
        public void FindGaps_ResponseWithOnlyTheDefaultReasonPhrase_ReportsTheOperationAndStatus()
        {
            // Arrange
            string document = DocumentedDocument.Replace("The identifier is malformed.", "Bad Request");

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("GET /a").And.Contain("400");
        }

        [TestMethod]
        public void FindGaps_ParameterWithoutDescription_ReportsTheOperationAndParameter()
        {
            // Arrange
            string document = DocumentedDocument.Replace(", \"description\": \"The identifier.\"", string.Empty);

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("GET /a").And.Contain("'id'");
        }

        [TestMethod]
        public void FindGaps_RequestBodyWithoutDescription_ReportsTheOperation()
        {
            // Arrange
            string document = DocumentedDocument.Replace("{ \"description\": \"The payload.\" }", "{ }");

            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(document);

            // Assert
            gaps.Should().ContainSingle().Which.Should().Contain("POST /b").And.Contain("request body");
        }

        [TestMethod]
        public void FindGaps_ProblemDetailsWithoutDescriptions_IsExempt()
        {
            // Act
            IReadOnlyList<string> gaps = OpenApiDocumentationCheck.FindGaps(DocumentedDocument);

            // Assert
            gaps.Should().NotContain(g => g.Contains("ProblemDetails"));
        }
    }
}