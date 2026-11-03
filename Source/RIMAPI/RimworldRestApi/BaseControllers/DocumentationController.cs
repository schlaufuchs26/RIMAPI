using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Core
{
    public class DocumentationController
    {
        private readonly IDocumentationService _docService;
        private readonly string _modFolderPath;

        public DocumentationController(IDocumentationService docService)
        {
            _docService = docService;
            _modFolderPath = GetModFolderPath();
        }

        [Get("/api/v1/core/docs/export")]
        [EndpointMetadata("Export the core API documentation", Notes = "The body is the document itself, not the API envelope.")]
        [ResponseBody(typeof(string))]
        public async Task ExportCoreDocumentation(HttpListenerContext context)
        {
            await ExportDocumentation(context, true);
        }

        [Get("/api/v1/docs")]
        [QueryParameters("format:string?")]
        [EndpointMetadata("Get the API documentation", Notes = "The body is the document itself in the format the query asks for, not the API envelope.")]
        [ResponseBody(typeof(string))]
        public async Task GetDocumentation(HttpListenerContext context)
        {
            var format = context.Request.QueryString["format"] ?? "html";

            try
            {
                switch (format.ToLower())
                {
                    case "json":
                        var docs = _docService.GenerateDocumentation();
                        await SendRawResponse(
                            context.Response,
                            JsonConvert.SerializeObject(docs, Formatting.Indented),
                            "application/json"
                        );
                        break;

                    case "markdown":
                        var markdown = _docService.GenerateMarkdown();
                        await SendRawResponse(context.Response, markdown, "text/markdown");
                        break;

                    case "openapi":
                        await SendRawResponse(
                            context.Response,
                            _docService.GenerateOpenApiYaml(),
                            "application/yaml"
                        );
                        break;

                    case "openapi-json":
                        await SendRawResponse(
                            context.Response,
                            _docService.GenerateOpenApiJson(),
                            "application/json"
                        );
                        break;

                    default:
                        var html = GenerateHtmlDocumentation();
                        await SendRawResponse(context.Response, html, "text/html");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error generating documentation: {ex}");
                await ResponseBuilder.SendError(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    "Failed to generate documentation"
                );
            }
        }

        // The document routes answer the document itself, so the payload this
        // helper sends is text whatever the format (fox ticket #4304).
        [ResponseBody(typeof(string))]
        private async Task SendRawResponse(
            HttpListenerResponse response,
            string content,
            string contentType
        )
        {
            try
            {
                response.StatusCode = 200;
                response.ContentType = contentType;
                response.ContentEncoding = Encoding.UTF8;

                var buffer = Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error sending raw response: {ex}");
                throw;
            }
        }

        private string GenerateHtmlDocumentation()
        {
            var markdown = _docService.GenerateMarkdown();

            // Simple markdown to HTML conversion
            var html = markdown
                .Replace("# ", "<h1>")
                .Replace("\n#", "</h1>\n<h1>")
                .Replace("## ", "<h2>")
                .Replace("\n##", "</h2>\n<h2>")
                .Replace("### ", "<h3>")
                .Replace("\n###", "</h3>\n<h3>")
                .Replace("```json", "<pre><code class=\"language-json\">")
                .Replace("```", "</code></pre>")
                .Replace("**", "<strong>")
                .Replace("**", "</strong>")
                .Replace("\n\n", "</p><p>")
                .Replace("\n", "<br/>");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>RimWorld REST API Documentation</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }}
        pre {{ background: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }}
        code {{ background: #f4f4f4; padding: 2px 5px; border-radius: 3px; }}
        table {{ border-collapse: collapse; width: 100%; margin: 15px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; }}
        .endpoint {{ background: #e8f4fd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
        .method-get {{ color: #00a000; }}
        .method-post {{ color: #ffa500; }}
        .method-put {{ color: #007bff; }}
        .method-delete {{ color: #dc3545; }}
    </style>
</head>
<body>
    {html}
</body>
</html>";
        }

        // Additional endpoint for specific extension documentation
        [Get("/api/v1/docs/extensions/{extensionId}")]
        [EndpointMetadata("Get one extension's documentation", Notes = "The body is the extension's own document as JSON, not the API envelope.")]
        [ResponseBody(typeof(string))]
        public async Task GetExtensionDocumentation(HttpListenerContext context,
            [ParameterDescription("The extension's id, as the extension list names it.")] string extensionId
        )
        {
            try
            {
                var docs = _docService.GenerateDocumentation();
                var extensionSection = docs.Sections.FirstOrDefault(s =>
                    s.Name.Replace(" ", "").Equals(extensionId, StringComparison.OrdinalIgnoreCase)
                );

                if (extensionSection == null)
                {
                    await ResponseBuilder.SendError(
                        context.Response,
                        HttpStatusCode.NotFound,
                        $"Extension '{extensionId}' not found"
                    );
                    return;
                }

                // A document like its siblings, not an envelope: one schema
                // then covers every route of this controller (fox ticket #4304).
                await SendRawResponse(
                    context.Response,
                    JsonConvert.SerializeObject(extensionSection, Formatting.Indented),
                    "application/json"
                );
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error generating extension documentation: {ex}");
                await ResponseBuilder.SendError(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    "Failed to generate extension documentation"
                );
            }
        }

        // Health check endpoint for documentation service
        [Get("/api/v1/docs/health")]
        [EndpointMetadata("Get the documentation health")]
        [ResponseBody(typeof(DocumentationHealthDto))]
        public async Task GetHealth(HttpListenerContext context)
        {
            try
            {
                var docs = _docService.GenerateDocumentation();
                var healthInfo = new DocumentationHealthDto
                {
                    Status = "healthy",
                    GeneratedAt = DateTime.UtcNow,
                    TotalEndpoints = docs.Sections.Sum(s => s.Endpoints.Count),
                    TotalExtensions = docs.Sections.Count - 1, // minus core
                    Sections = docs.Sections.Select(s => s.Name).ToList(),
                };

                await context.SendJsonResponse(ApiResult<DocumentationHealthDto>.Ok(healthInfo));
            }
            catch (Exception ex)
            {
                await ResponseBuilder.SendError(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    $"Documentation service unhealthy: {ex.Message}"
                );
            }
        }

        [Get("/api/v1/docs/export")]
        [QueryParameters("format:string?")]
        [EndpointMetadata("Export the API documentation", Notes = "The body is the document itself, not the API envelope.")]
        [ResponseBody(typeof(string))]
        public async Task ExportDocumentation(HttpListenerContext context,
            [ParameterDescription("When true, the document is also written to a file on the server.")] bool saveFile = false
        )
        {
            try
            {
                var format = context.Request.QueryString["format"] ?? "markdown";
                string content;
                string fileName;
                string contentType;

                switch (format.ToLower())
                {
                    case "json":
                        var docs = _docService.GenerateDocumentation();
                        content = JsonConvert.SerializeObject(docs, Formatting.Indented);
                        fileName = "api.json";
                        contentType = "application/json";
                        break;

                    case "markdown":
                        content = _docService.GenerateMarkdown();
                        fileName = "api.md";
                        contentType = "text/markdown";
                        break;

                    case "openapi":
                        content = _docService.GenerateOpenApiYaml();
                        fileName = "openapi.yaml";
                        contentType = "application/yaml";
                        break;

                    case "openapi-json":
                        content = _docService.GenerateOpenApiJson();
                        fileName = "openapi.json";
                        contentType = "application/json";
                        break;

                    default:
                        content = _docService.GenerateMarkdown();
                        fileName = "api.md";
                        contentType = "text/markdown";
                        break;
                }

                if (saveFile)
                {
                    // Save to mod docs folder
                    var filePath = Path.Combine(_modFolderPath, "docs", fileName);
                    SaveToFile(filePath, content);
                    LogApi.Info($"Documentation exported to: {filePath}");
                }

                // Also return the content in response
                await SendRawResponse(context.Response, content, contentType);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error exporting documentation: {ex}");
                await ResponseBuilder.SendError(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    $"Failed to export documentation: {ex.Message}"
                );
            }
        }

        private string GetModFolderPath()
        {
            try
            {
                // Get the path to the mods folder
                var modsFolder = GenFilePaths.ModsFolderPath;

                // Try to find current mod folder by looking for our assembly
                var currentAssembly = Assembly.GetExecutingAssembly();
                var currentMod = LoadedModManager.RunningMods.FirstOrDefault(mod =>
                {
                    try
                    {
                        return mod.assemblies.loadedAssemblies.Contains(currentAssembly);
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (currentMod != null)
                {
                    return currentMod.RootDir;
                }

                // Fallback: use RIMAPI folder name
                var rimapiFolder = Path.Combine(modsFolder, "RIMAPI");
                if (Directory.Exists(rimapiFolder))
                {
                    return rimapiFolder;
                }

                // Last resort: use mods folder
                return modsFolder;
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting mod folder path: {ex}");
                return GenFilePaths.ModsFolderPath;
            }
        }

        private void SaveToFile(string filePath, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);
                LogApi.Info($"Successfully saved documentation to: {filePath}");
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error saving documentation to file {filePath}: {ex}");
                throw;
            }
        }
    }
}
