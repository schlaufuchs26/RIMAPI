using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RIMAPI.Core;

public static class HttpListenerRequestExtensions
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new SnakeCaseContractResolver(),
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>
    /// Read and deserialize the request body as JSON
    /// </summary>
    public static async Task<T> ReadBodyAsync<T>(this HttpListenerRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!request.HasEntityBody)
            return default(T);

        try
        {
            // A JSON body is UTF-8 unless its Content-Type names a charset.
            // HttpListener's ContentEncoding falls back to the platform's
            // default when none is named, and that turned a UTF-8 body into
            // question marks: a colony called "Die Füchse" arrived as "Die
            // F??chse" and the game refused the name (fox ticket #3929).
            Encoding encoding = request.ContentEncoding ?? Encoding.UTF8;
            string contentType = request.ContentType;
            if (contentType == null ||
                contentType.IndexOf("charset=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                encoding = Encoding.UTF8;
            }
            using (var reader = new StreamReader(request.InputStream, encoding))
            {
                var json = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(json))
                    return default(T);

                LogApi.Message($"Receive body: {json}", LoggingLevels.DEBUG);

                return JsonConvert.DeserializeObject<T>(json, JsonSettings);
            }
        }
        catch (Exception ex)
        {
            LogApi.Error($"Error reading request body at {request.Url}: {ex}");
            return default(T);
        }
    }

    /// <summary>
    /// Read the request body as string
    /// </summary>
    public static async Task<string> ReadBodyAsStringAsync(this HttpListenerRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!request.HasEntityBody)
            return string.Empty;

        try
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            LogApi.Error($"Error reading request body as string at {request.Url}: {ex}");
            return string.Empty;
        }
    }
}