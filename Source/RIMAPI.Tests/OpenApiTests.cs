using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The OpenAPI renderer (fox ticket #4234): the mod's route registry is
    /// its ApiExplorer, so the document is only as good as the attributes on
    /// the handlers. These tests pin the shape a client reads: paths, tags,
    /// query parameters, the body reference and the DTO schemas.
    /// </summary>
    public class OpenApiTests
    {
        [Fact]
        public void AQueryParameterSpecReadsNameTypeAndOptionality()
        {
            QueryParameterSpec required = QueryParameterSpec.Parse("map_id:integer");
            Assert.Equal("map_id", required.Name);
            Assert.Equal("integer", required.Type);
            Assert.True(required.Required);

            QueryParameterSpec optional = QueryParameterSpec.Parse("detailed:boolean?");
            Assert.Equal("detailed", optional.Name);
            Assert.Equal("boolean", optional.Type);
            Assert.False(optional.Required);
        }

        [Theory]
        [InlineData("map_id")]
        [InlineData("map_id:")]
        [InlineData(":integer")]
        [InlineData("map_id:text")]
        [InlineData("map id:integer")]
        public void ABadQueryParameterSpecNamesTheProblem(string raw)
        {
            ArgumentException error = Assert.Throws<ArgumentException>(
                () => QueryParameterSpec.Parse(raw)
            );
            Assert.Contains("query parameter", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TheYamlWriterQuotesWhatWouldOtherwiseChangeMeaning()
        {
            var tree = new JObject
            {
                ["openapi"] = "3.1.0",
                ["number-as-string"] = "200",
                ["plain"] = "The map's id",
                ["empty"] = "",
                ["nothing"] = null,
                ["flag"] = true,
                ["empty-map"] = new JObject(),
                ["empty-list"] = new JArray(),
            };

            string yaml = YamlWriter.Write(tree);

            Assert.Contains("openapi: 3.1.0\n", yaml);
            Assert.Contains("number-as-string: \"200\"\n", yaml);
            Assert.Contains("plain: \"The map's id\"\n", yaml);
            Assert.Contains("empty: \"\"\n", yaml);
            Assert.Contains("nothing: null\n", yaml);
            Assert.Contains("flag: true\n", yaml);
            Assert.Contains("empty-map: {}\n", yaml);
            Assert.Contains("empty-list: []\n", yaml);
        }

        [Fact]
        public void TheYamlWriterIndentsNestedMapsAndSequences()
        {
            var tree = new JObject
            {
                ["paths"] = new JObject
                {
                    ["/api/v1/things"] = new JObject
                    {
                        ["get"] = new JObject
                        {
                            ["tags"] = new JArray("Things"),
                            ["parameters"] = new JArray(
                                new JObject { ["name"] = "map_id", ["in"] = "query" }
                            ),
                        },
                    },
                },
            };

            string yaml = YamlWriter.Write(tree);

            Assert.Equal(
                "paths:\n"
                    + "  /api/v1/things:\n"
                    + "    get:\n"
                    + "      tags:\n"
                    + "        - Things\n"
                    + "      parameters:\n"
                    + "        - name: map_id\n"
                    + "          in: query\n",
                yaml
            );
        }

        [Fact]
        public void TheDocumentCarriesEveryRouteWithItsTagParametersAndBody()
        {
            ApiDocumentation docs = Fixture();

            JObject document = OpenApiDocument.Build(docs);

            Assert.Equal("3.1.0", (string)document["openapi"]);
            Assert.Equal("http://localhost:8765", (string)document["servers"][0]["url"]);
            Assert.Equal("1.10.0", (string)document["info"]["version"]);

            JObject paths = (JObject)document["paths"];
            Assert.NotNull(paths["/api/v1/things"]);
            Assert.NotNull(paths["/api/v1/things/{id}"]);

            JObject get = (JObject)paths["/api/v1/things"]["get"];
            Assert.Equal("Things", (string)get["tags"][0]);
            Assert.Equal("List things", (string)get["summary"]);

            JObject mapId = (JObject)get["parameters"]
                .First(p => (string)p["name"] == "map_id");
            Assert.Equal("query", (string)mapId["in"]);
            Assert.Equal("integer", (string)mapId["schema"]["type"]);
            Assert.True((bool)mapId["required"]);

            JObject detailed = (JObject)get["parameters"]
                .First(p => (string)p["name"] == "detailed");
            Assert.False((bool)detailed["required"]);

            JObject post = (JObject)paths["/api/v1/things"]["post"];
            Assert.Equal(
                "#/components/schemas/OpenApiTestRequest",
                (string)post["requestBody"]["content"]["application/json"]["schema"]["$ref"]
            );

            JObject byId = (JObject)paths["/api/v1/things/{id}"]["get"];
            JObject id = (JObject)byId["parameters"].First(p => (string)p["name"] == "id");
            Assert.Equal("path", (string)id["in"]);
            Assert.True((bool)id["required"]);
        }

        [Fact]
        public void TheDtoSchemasUseTheWireNamesAndTheWireTypes()
        {
            JObject document = OpenApiDocument.Build(Fixture());
            JObject schemas = (JObject)document["components"]["schemas"];

            JObject request = (JObject)schemas["OpenApiTestRequest"];
            Assert.Equal("object", (string)request["type"]);
            Assert.NotNull(request["properties"]["thing_def"]);
            Assert.Equal("string", (string)request["properties"]["thing_def"]["type"]);
            Assert.Equal("integer", (string)request["properties"]["count"]["type"]);
            Assert.Equal("boolean", (string)request["properties"]["forbidden"]["type"]);
            Assert.Equal("integer", (string)request["properties"]["kind"]["type"]);
            Assert.Equal("array", (string)request["properties"]["nested"]["type"]);
            Assert.Equal(
                "#/components/schemas/OpenApiTestRequest",
                (string)request["properties"]["nested"]["items"]["$ref"]
            );
            Assert.Contains("count", request["required"].Select(t => (string)t));

            Assert.NotNull(schemas["ApiResult"]);
        }

        [Fact]
        public void TheEnvelopeDocumentsSuccessErrorsAndTheTick()
        {
            JObject schemas = (JObject)OpenApiDocument.Build(Fixture())["components"]["schemas"];
            JObject envelope = (JObject)schemas["ApiResult"];

            Assert.Equal("boolean", (string)envelope["properties"]["success"]["type"]);
            Assert.Equal("array", (string)envelope["properties"]["errors"]["type"]);
            Assert.Equal("date-time", (string)envelope["properties"]["timestamp"]["format"]);
            Assert.Equal("integer", (string)envelope["properties"]["tick"]["type"]);
        }

        [Fact]
        public void TheYamlAndJsonFormsDescribeTheSameDocument()
        {
            ApiDocumentation docs = Fixture();

            JObject fromJson = JObject.Parse(OpenApiDocument.ToJson(docs));
            JObject built = OpenApiDocument.Build(docs);
            string yaml = OpenApiDocument.ToYaml(docs);

            Assert.True(JToken.DeepEquals(built, fromJson));
            Assert.StartsWith("openapi: 3.1.0\n", yaml);
            Assert.Contains("      operationId: ListThings\n", yaml);
            Assert.Contains("        - name: map_id\n", yaml);
        }

        [Fact]
        public void ADeclaredResponseTypeBecomesTheEnvelopesDataSchema()
        {
            ApiDocumentation docs = Fixture();
            docs.Sections[0].Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "GET",
                    Path = "/api/v1/things/typed",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "ListTypedThings",
                    DeclaresResponseBody = true,
                    ResponseBodyType = typeof(List<OpenApiTestRequest>),
                }
            );

            JObject document = OpenApiDocument.Build(docs);
            JObject operation = (JObject)document["paths"]["/api/v1/things/typed"]["get"];
            string reference = (string)
                operation["responses"]["200"]["content"]["application/json"]["schema"]["$ref"];

            Assert.Equal("#/components/schemas/ApiResultOfListOfOpenApiTestRequest", reference);

            JObject envelope = (JObject)document["components"]["schemas"]["ApiResultOfListOfOpenApiTestRequest"];
            Assert.Equal("object", (string)envelope["type"]);
            Assert.False((bool)envelope["additionalProperties"]);
            Assert.Equal("boolean", (string)envelope["properties"]["success"]["type"]);
            Assert.Equal("array", (string)envelope["properties"]["data"]["type"]);
            Assert.Equal(
                "#/components/schemas/OpenApiTestRequest",
                (string)envelope["properties"]["data"]["items"]["$ref"]
            );
        }

        [Fact]
        public void ADeclaredRouteWithoutAPayloadSaysSo()
        {
            ApiDocumentation docs = Fixture();
            docs.Sections[0].Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "POST",
                    Path = "/api/v1/things/nothing",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "DoNothing",
                    DeclaresResponseBody = true,
                }
            );

            JObject document = OpenApiDocument.Build(docs);
            JObject operation = (JObject)document["paths"]["/api/v1/things/nothing"]["post"];
            Assert.Equal(
                "#/components/schemas/ApiResultNoData",
                (string)operation["responses"]["200"]["content"]["application/json"]["schema"]["$ref"]
            );

            JObject envelope = (JObject)document["components"]["schemas"]["ApiResultNoData"];
            Assert.Equal("null", (string)envelope["properties"]["data"]["type"]);
        }

        [Fact]
        public void AnEnumSchemaNamesTheValuesTheWireCarries()
        {
            ApiDocumentation docs = Fixture();
            docs.Sections[0].Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "GET",
                    Path = "/api/v1/things/kind",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "GetKind",
                    DeclaresResponseBody = true,
                    ResponseBodyType = typeof(OpenApiTestRequest),
                }
            );

            JObject document = OpenApiDocument.Build(docs);
            JObject kind = (JObject)
                document["components"]["schemas"]["OpenApiTestRequest"]["properties"]["kind"];

            Assert.Equal("integer", (string)kind["type"]);
            Assert.Equal(new[] { 0L, 1L }, kind["enum"].Select(token => (long)token).ToArray());
            Assert.Equal(
                new[] { "Item", "Building" },
                kind["x-enum-varnames"].Select(token => (string)token).ToArray()
            );
        }

        /// <summary>
        /// The gate the ticket asks for (fox ticket #4304): no schema in the
        /// document may be a bare object, and no property may be untyped. A
        /// route that has not declared its response yet still points at the
        /// untyped envelope, which is why the route list is a ratchet in
        /// ResponseSchemaTests rather than a check on the rendered document.
        /// </summary>
        [Fact]
        public void NoSchemaInTheDocumentIsABareObject()
        {
            ApiDocumentation docs = Fixture();
            docs.Sections[0].Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "GET",
                    Path = "/api/v1/things/typed",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "ListTypedThings",
                    DeclaresResponseBody = true,
                    ResponseBodyType = typeof(List<OpenApiTestRequest>),
                }
            );

            var problems = new List<string>();
            Inspect(OpenApiDocument.Build(docs), "components", problems);
            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        /// <summary>
        /// Walks every schema node: an object needs properties (or an explicit
        /// additionalProperties schema), and a node needs a type, a $ref or a
        /// composition keyword.
        /// </summary>
        private static void Inspect(JToken node, string at, List<string> problems)
        {
            JObject schema = node as JObject;
            if (schema != null)
            {
                bool looksLikeSchema =
                    schema["type"] != null
                    || schema["$ref"] != null
                    || schema["properties"] != null
                    || schema["items"] != null;

                if (looksLikeSchema)
                {
                    if (schema["type"] != null && (string)schema["type"] == "object"
                        && schema["properties"] == null
                        && schema["additionalProperties"] == null)
                        problems.Add(at + " is a bare object without properties");
                    if (schema["type"] == null && schema["$ref"] == null
                        && schema["anyOf"] == null && schema["allOf"] == null
                        && schema["oneOf"] == null && schema["enum"] == null
                        && schema["description"] == null)
                        problems.Add(at + " declares no type");
                }

                foreach (JProperty property in schema.Properties())
                    Inspect(property.Value, at + "/" + property.Name, problems);
                return;
            }

            JArray array = node as JArray;
            if (array != null)
            {
                for (int i = 0; i < array.Count; i++)
                    Inspect(array[i], at + "[" + i + "]", problems);
            }
        }

        private static ApiDocumentation Fixture()
        {
            var docs = new ApiDocumentation
            {
                BaseUrl = "http://localhost:8765/api/v1",
                Version = "1.10.0",
            };
            var section = new DocumentationSection
            {
                Name = "Core API",
                Description = "Built-in endpoints",
            };
            section.Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "GET",
                    Path = "/api/v1/things",
                    Description = "List things",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "ListThings",
                    Tags = new[] { "Things" },
                    Parameters = new List<EndpointParameter>
                    {
                        new EndpointParameter
                        {
                            Name = "map_id",
                            Type = "integer",
                            Source = "query",
                            Required = true,
                        },
                        new EndpointParameter
                        {
                            Name = "detailed",
                            Type = "boolean",
                            Source = "query",
                            Required = false,
                        },
                    },
                }
            );
            section.Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "POST",
                    Path = "/api/v1/things",
                    Description = "Spawn a thing",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "SpawnThing",
                    RequestBodyType = typeof(OpenApiTestRequest),
                }
            );
            section.Endpoints.Add(
                new DocumentedEndpoint
                {
                    Method = "GET",
                    Path = "/api/v1/things/{id}",
                    Description = "One thing",
                    Category = "ThingsController",
                    Controller = "ThingsController",
                    Action = "GetThing",
                    Parameters = new List<EndpointParameter>
                    {
                        new EndpointParameter
                        {
                            Name = "id",
                            Type = "Int32",
                            Source = "path",
                            Required = true,
                        },
                    },
                }
            );
            docs.Sections.Add(section);
            return docs;
        }
    }
}

namespace RIMAPI.Models
{
    /// <summary>
    /// A fixture DTO, in the models namespace on purpose: the schema builder
    /// only expands types the API actually serializes, which is RIMAPI.Models
    /// (a game type would drag the whole game into the document).
    /// </summary>
    public enum OpenApiTestKind
    {
        Item = 0,
        Building = 1,
    }

    public class OpenApiTestRequest
    {
        public string ThingDef { get; set; }
        public int Count { get; set; }
        public bool Forbidden { get; set; }
        public OpenApiTestKind Kind { get; set; }
        public List<OpenApiTestRequest> Nested { get; set; }
    }
}
