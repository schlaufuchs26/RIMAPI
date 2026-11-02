using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RIMAPI.Core
{
    /// <summary>
    /// Renders the mod's route documentation as an OpenAPI 3.1 document.
    /// </summary>
    /// <remarks>
    /// Swashbuckle and NSwag build their document from ASP.NET's ApiExplorer;
    /// this mod has its own HttpListener router, so the route registry is the
    /// ApiExplorer here (ticket #4234). Everything the document needs beyond
    /// the route itself is declared on the handler: [RequestBody(typeof(T))]
    /// for the body DTO, [QueryParameters(...)] for the query string.
    ///
    /// The document is a Newtonsoft tree so the same builder serves YAML and
    /// JSON; <see cref="YamlWriter"/> turns the tree into the YAML form.
    /// </remarks>
    public static class OpenApiDocument
    {
        public const string Version = "3.1.0";

        public static JObject Build(ApiDocumentation documentation)
        {
            if (documentation == null)
                throw new ArgumentNullException(nameof(documentation));

            var schemas = new SchemaBuilder();
            var paths = new JObject();
            var tagNames = new List<string>();
            var operationIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (DocumentationSection section in documentation.Sections)
            {
                foreach (DocumentedEndpoint endpoint in section.Endpoints)
                {
                    string path = NormalizePath(endpoint.Path);
                    string method = (endpoint.Method ?? "GET").ToLowerInvariant();

                    var pathItem = paths[path] as JObject;
                    if (pathItem == null)
                    {
                        pathItem = new JObject();
                        paths[path] = pathItem;
                    }

                    if (pathItem[method] != null)
                        throw new InvalidOperationException(
                            "Duplicate route " + method.ToUpperInvariant() + " " + path
                        );

                    JObject operation = BuildOperation(endpoint, schemas, operationIds);
                    pathItem[method] = operation;

                    foreach (JToken tag in (JArray)operation["tags"])
                    {
                        string name = (string)tag;
                        if (!tagNames.Contains(name))
                            tagNames.Add(name);
                    }
                }
            }

            schemas.AddEnvelope();

            var document = new JObject
            {
                ["openapi"] = Version,
                ["info"] = new JObject
                {
                    ["title"] = "RimWorld RIMAPI",
                    ["version"] = documentation.Version ?? "0.0.0",
                    ["description"] =
                        "The mod's HTTP surface, rendered from its own route registry. "
                        + "Every route answers the same envelope: success is false on a refusal, "
                        + "and that refusal is still HTTP 200 with the reason in errors. "
                        + "Field names are snake_case. A collection route also accepts the "
                        + "filter keys include and filter (comma-separated), which keep only "
                        + "the items whose named fields match.",
                },
                ["servers"] = new JArray
                {
                    new JObject { ["url"] = ServerUrl(documentation.BaseUrl) },
                },
                ["tags"] = new JArray(
                    tagNames.Select(name => (JToken)new JObject { ["name"] = name }).ToArray()
                ),
                ["paths"] = paths,
                ["components"] = new JObject { ["schemas"] = schemas.Components },
            };

            return document;
        }

        /// <summary>The OpenAPI document as YAML, the form a spec checker reads.</summary>
        public static string ToYaml(ApiDocumentation documentation) =>
            YamlWriter.Write(Build(documentation));

        /// <summary>The same document as JSON, for clients that would rather parse JSON.</summary>
        public static string ToJson(ApiDocumentation documentation) =>
            Build(documentation).ToString(Formatting.Indented);

        private static JObject BuildOperation(
            DocumentedEndpoint endpoint,
            SchemaBuilder schemas,
            HashSet<string> operationIds
        )
        {
            var tags = new JArray();
            string[] declared = endpoint.Tags ?? Array.Empty<string>();
            if (declared.Length > 0)
            {
                foreach (string tag in declared)
                    if (!string.IsNullOrWhiteSpace(tag))
                        tags.Add(tag);
            }
            if (tags.Count == 0)
                tags.Add(string.IsNullOrWhiteSpace(endpoint.Category) ? "General" : endpoint.Category);

            var operation = new JObject
            {
                ["tags"] = tags,
                ["operationId"] = UniqueOperationId(endpoint, operationIds),
            };

            if (!string.IsNullOrWhiteSpace(endpoint.Description))
                operation["summary"] = endpoint.Description;
            if (!string.IsNullOrWhiteSpace(endpoint.Notes))
                operation["description"] = endpoint.Notes;
            if (!string.IsNullOrWhiteSpace(endpoint.DeprecationNotice))
                operation["deprecated"] = true;

            JArray parameters = BuildParameters(endpoint);
            if (parameters.Count > 0)
                operation["parameters"] = parameters;

            if (endpoint.RequestBodyType != null)
            {
                operation["requestBody"] = new JObject
                {
                    ["required"] = true,
                    ["content"] = new JObject
                    {
                        ["application/json"] = new JObject
                        {
                            ["schema"] = schemas.Build(endpoint.RequestBodyType),
                        },
                    },
                };
            }

            operation["responses"] = new JObject
            {
                ["200"] = new JObject
                {
                    ["description"] =
                        "The API envelope. A refusal is also HTTP 200, with success=false "
                        + "and the reason in errors.",
                    ["content"] = new JObject
                    {
                        ["application/json"] = new JObject
                        {
                            ["schema"] = schemas.Envelope(endpoint),
                        },
                    },
                },
            };

            return operation;
        }

        private static JArray BuildParameters(DocumentedEndpoint endpoint)
        {
            var parameters = new JArray();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (EndpointParameter parameter in endpoint.Parameters)
            {
                string location = parameter.Source == "path" ? "path" : "query";
                if (location == "query" && parameter.Source != "query")
                    continue; // a method argument the route pattern does not carry is not documented
                if (string.IsNullOrWhiteSpace(parameter.Name))
                    continue;

                string key = location + ":" + parameter.Name;
                if (!seen.Add(key))
                    continue;

                var schema = new JObject { ["type"] = QueryType(parameter.Type) };
                var entry = new JObject
                {
                    ["name"] = parameter.Name,
                    ["in"] = location,
                    ["required"] = location == "path" || parameter.Required,
                    ["schema"] = schema,
                };
                if (!string.IsNullOrWhiteSpace(parameter.Description))
                    entry["description"] = parameter.Description;

                parameters.Add(entry);
            }

            // A templated path segment with no declared argument still needs its
            // parameter, or the document is invalid.
            foreach (string name in PathParameters(endpoint.Path))
            {
                if (!seen.Add("path:" + name))
                    continue;

                parameters.Add(
                    new JObject
                    {
                        ["name"] = name,
                        ["in"] = "path",
                        ["required"] = true,
                        ["schema"] = new JObject { ["type"] = "string" },
                    }
                );
            }

            return parameters;
        }

        private static IEnumerable<string> PathParameters(string path)
        {
            if (string.IsNullOrEmpty(path))
                yield break;

            int index = 0;
            while (true)
            {
                int open = path.IndexOf('{', index);
                if (open < 0)
                    yield break;
                int close = path.IndexOf('}', open + 1);
                if (close < 0)
                    yield break;

                string name = path.Substring(open + 1, close - open - 1);
                // A route pattern may carry a type constraint ({id:int}); the
                // parameter's name is the part before the colon.
                int colon = name.IndexOf(':');
                if (colon >= 0)
                    name = name.Substring(0, colon);
                if (name.Length > 0)
                    yield return name;

                index = close + 1;
            }
        }

        private static string UniqueOperationId(
            DocumentedEndpoint endpoint,
            HashSet<string> operationIds
        )
        {
            string baseId = string.IsNullOrWhiteSpace(endpoint.Action)
                ? (endpoint.Method + endpoint.Path).Replace("/", "_")
                : endpoint.Action;

            string candidate = baseId;
            if (!operationIds.Add(candidate))
            {
                string controller = string.IsNullOrWhiteSpace(endpoint.Controller)
                    ? endpoint.Category
                    : endpoint.Controller;
                candidate = (controller ?? "Route") + "." + baseId;
                int suffix = 2;
                string unique = candidate;
                while (!operationIds.Add(unique))
                    unique = candidate + "_" + suffix++;
                candidate = unique;
            }

            return candidate;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A route needs a path.", nameof(path));

            return path[0] == '/' ? path : "/" + path;
        }

        private static string ServerUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "/";

            string url = baseUrl.TrimEnd('/');
            const string suffix = "/api/v1";
            if (url.EndsWith(suffix, StringComparison.Ordinal))
                url = url.Substring(0, url.Length - suffix.Length);
            return url.Length == 0 ? "/" : url;
        }

        /// <summary>Maps the model's display type names onto JSON schema types.</summary>
        private static string QueryType(string type)
        {
            switch (type)
            {
                case "integer":
                case "int":
                case "Int32":
                case "long":
                case "Int64":
                    return "integer";
                case "number":
                case "float":
                case "double":
                case "decimal":
                case "Single":
                case "Double":
                    return "number";
                case "boolean":
                case "bool":
                case "Boolean":
                    return "boolean";
                default:
                    return "string";
            }
        }

        /// <summary>
        /// Builds component schemas from DTO types, the way the API serializes
        /// them: snake_case names from <see cref="SnakeCaseContractResolver"/>,
        /// enums as integers (no StringEnumConverter on the response path).
        /// </summary>
        private sealed class SchemaBuilder
        {
            private static readonly SnakeCaseContractResolver Resolver = new SnakeCaseContractResolver();
            private readonly Dictionary<Type, string> _names = new Dictionary<Type, string>();
            private readonly Dictionary<string, Type> _owners = new Dictionary<string, Type>(StringComparer.Ordinal);
            private readonly HashSet<Type> _building = new HashSet<Type>();

            public JObject Components { get; } = new JObject();

            public JToken Build(Type type)
            {
                if (type == null)
                    return new JObject();

                Type nullable = Nullable.GetUnderlyingType(type);
                if (nullable != null)
                {
                    return new JObject
                    {
                        ["anyOf"] = new JArray(Build(nullable), new JObject { ["type"] = "null" }),
                    };
                }

                if (type == typeof(string) || type == typeof(char))
                    return new JObject { ["type"] = "string" };
                if (type == typeof(bool))
                    return new JObject { ["type"] = "boolean" };
                if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short)
                    || type == typeof(ushort) || type == typeof(int) || type == typeof(uint))
                    return new JObject { ["type"] = "integer", ["format"] = "int32" };
                if (type == typeof(long) || type == typeof(ulong))
                    return new JObject { ["type"] = "integer", ["format"] = "int64" };
                if (type == typeof(float))
                    return new JObject { ["type"] = "number", ["format"] = "float" };
                if (type == typeof(double) || type == typeof(decimal))
                    return new JObject { ["type"] = "number", ["format"] = "double" };
                if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                    return new JObject { ["type"] = "string", ["format"] = "date-time" };
                if (type == typeof(Guid))
                    return new JObject { ["type"] = "string", ["format"] = "uuid" };
                if (type == typeof(TimeSpan))
                    return new JObject { ["type"] = "string" };
                if (type == typeof(byte[]))
                    return new JObject { ["type"] = "string", ["format"] = "byte" };
                if (type.IsEnum)
                    return new JObject
                    {
                        ["type"] = "integer",
                        ["format"] = "int32",
                        // The response path serializes an enum as its numeric
                        // value (no StringEnumConverter), so the schema names
                        // the numbers the wire carries and keeps the member
                        // names beside them for a reader of the document.
                        ["enum"] = new JArray(
                            Enum.GetValues(type)
                                .Cast<object>()
                                .Select(value => (JToken)Convert.ToInt64(value))
                                .ToArray()
                        ),
                        ["x-enum-varnames"] = new JArray(Enum.GetNames(type)),
                    };
                if (type == typeof(object) || typeof(JToken).IsAssignableFrom(type))
                    return new JObject();

                if (type.IsArray)
                    return new JObject { ["type"] = "array", ["items"] = Build(type.GetElementType()) };

                if (type.IsGenericType)
                {
                    Type definition = type.GetGenericTypeDefinition();
                    if (definition == typeof(List<>) || definition == typeof(IList<>)
                        || definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>)
                        || definition == typeof(IReadOnlyList<>) || definition == typeof(HashSet<>))
                        return new JObject
                        {
                            ["type"] = "array",
                            ["items"] = Build(type.GetGenericArguments()[0]),
                        };

                    if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>))
                    {
                        Type[] arguments = type.GetGenericArguments();
                        if (arguments[0] == typeof(string))
                            return new JObject
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = Build(arguments[1]),
                            };
                        return new JObject { ["type"] = "object" };
                    }
                }

                if (!IsModel(type))
                    return new JObject();

                return Reference(type);
            }

            public void AddEnvelope()
            {
                if (Components["ApiResult"] != null)
                    return;

                // Three envelope shapes: untyped (a route that has not declared
                // its response yet), no payload (declared with a bare
                // [ResponseBody]) and one per payload type, built on demand by
                // Envelope().
                var untyped = new JObject
                {
                    ["description"] = "The envelope every route answers with.",
                };
                FillEnvelope(
                    untyped,
                    new JObject
                    {
                        ["description"] =
                            "The route's payload; absent when the route reports nothing back.",
                    }
                );
                Components["ApiResult"] = untyped;

                var noData = new JObject { ["description"] = "The envelope of a route that reports no payload." };
                FillEnvelope(
                    noData,
                    new JObject
                    {
                        ["type"] = "null",
                        ["description"] = "Always null: the route answers success and errors only.",
                    }
                );
                Components["ApiResultNoData"] = noData;
            }

            /// <summary>
            /// The 200 schema of one route: the untyped envelope while a route
            /// declares nothing, and otherwise an envelope whose data field is
            /// the type the route answers (ticket #4304).
            /// </summary>
            public JToken Envelope(DocumentedEndpoint endpoint)
            {
                if (!endpoint.DeclaresResponseBody)
                    return Reference("ApiResult");

                if (endpoint.ResponseBodyType == null)
                    return Reference("ApiResultNoData");

                string name = "ApiResultOf" + TypeName(endpoint.ResponseBodyType);
                if (Components[name] == null)
                {
                    var schema = new JObject();
                    FillEnvelope(schema, Build(endpoint.ResponseBodyType));
                    Components[name] = schema;
                }

                return Reference(name);
            }

            private static JToken Reference(string component) =>
                new JObject { ["$ref"] = "#/components/schemas/" + component };

            private static void FillEnvelope(JObject schema, JToken data)
            {
                schema["type"] = "object";
                schema["additionalProperties"] = false;
                schema["required"] = new JArray("success", "timestamp", "tick");
                schema["properties"] = new JObject
                {
                    ["success"] = new JObject { ["type"] = "boolean" },
                    ["data"] = data,
                    ["errors"] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = new JObject { ["type"] = "string" },
                    },
                    ["warnings"] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = new JObject { ["type"] = "string" },
                    },
                    ["timestamp"] = new JObject { ["type"] = "string", ["format"] = "date-time" },
                    ["tick"] = new JObject
                    {
                        ["type"] = "integer",
                        ["format"] = "int32",
                        ["description"] = "The game tick the answer describes; 0 when no game is loaded.",
                    },
                };
            }

            /// <summary>
            /// A component name for a payload type: `PawnDto`, `ListOfPawnDto`,
            /// `DictionaryOfStringToListOfThingDto`. Stable across builds, so a
            /// generated client sees the same name for the same type.
            /// </summary>
            private static string TypeName(Type type)
            {
                Type nullable = Nullable.GetUnderlyingType(type);
                if (nullable != null)
                    return TypeName(nullable) + "Nullable";

                if (type.IsArray)
                    return TypeName(type.GetElementType()) + "Array";

                if (type.IsGenericType)
                {
                    Type definition = type.GetGenericTypeDefinition();
                    Type[] arguments = type.GetGenericArguments();
                    if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>))
                        return "DictionaryOf" + TypeName(arguments[0]) + "To" + TypeName(arguments[1]);
                    if (arguments.Length == 1)
                        return type.Name.Split('`')[0] + "Of" + TypeName(arguments[0]);
                }

                return type.Name;
            }

            private JToken Reference(Type type)
            {
                string name;
                if (!_names.TryGetValue(type, out name))
                {
                    name = type.Name;
                    Type owner;
                    if (_owners.TryGetValue(name, out owner) && owner != type)
                        name = type.FullName.Replace('+', '.');

                    _names[type] = name;
                    _owners[name] = type;

                    // Register before filling the properties so a self-reference
                    // resolves to the same component instead of recursing.
                    // additionalProperties:false makes the shape closed, so a
                    // generated client sees exactly the fields the route sends
                    // (ticket #4304).
                    var schema = new JObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                    };
                    Components[name] = schema;
                    Fill(type, schema);
                }

                return new JObject { ["$ref"] = "#/components/schemas/" + name };
            }

            private void Fill(Type type, JObject schema)
            {
                if (!_building.Add(type))
                    return;

                var properties = new JObject();
                var required = new JArray();

                var contract = Resolver.ResolveContract(type) as JsonObjectContract;
                if (contract != null)
                {
                    foreach (JsonProperty property in contract.Properties)
                    {
                        if (property.Ignored || string.IsNullOrEmpty(property.PropertyName))
                            continue;

                        properties[property.PropertyName] = Build(property.PropertyType);

                        // A field is required only when the serializer always
                        // writes it: a nullable or reference-typed field can be
                        // null and is then left out, and a field with
                        // DefaultValueHandling.Ignore is left out at its default
                        // (ticket #4304: StockRuleDto.Buys is true by default
                        // and absent from the wire).
                        bool nullable = !property.PropertyType.IsValueType
                            || Nullable.GetUnderlyingType(property.PropertyType) != null;
                        bool omittedAtDefault = property.DefaultValueHandling.HasValue
                            && (property.DefaultValueHandling.Value & DefaultValueHandling.Ignore)
                                == DefaultValueHandling.Ignore;

                        if (!nullable && !omittedAtDefault)
                            required.Add(property.PropertyName);
                    }
                }

                if (properties.Count > 0)
                    schema["properties"] = properties;
                if (required.Count > 0)
                    schema["required"] = required;

                _building.Remove(type);
            }

            private static bool IsModel(Type type)
            {
                if (type.IsPrimitive)
                    return false;

                string ns = type.Namespace ?? string.Empty;
                return ns == "RIMAPI.Models" || ns.StartsWith("RIMAPI.Models.", StringComparison.Ordinal);
            }
        }
    }
}
