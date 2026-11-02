using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Verse;

namespace RIMAPI.Core
{
    // SnakeCaseContractResolver lives in its own file (SnakeCaseContractResolver.cs)
    // so the unit tests can compile it without a game type (ticket #3316).

    // ApiResult classes for standardized responses
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        // Tick is the game tick this answer describes (fox ticket #4113): the
        // state behind the payload, stamped when the response goes out. The
        // fox's colony document keeps it per route, so a read that answered
        // from older state than the rest is visible instead of being a
        // disagreement nobody owns. 0 when no game is loaded.
        public int Tick { get; set; }

        public static ApiResult<T> Ok(T data) => new ApiResult<T> { Success = true, Data = data };

        public static ApiResult<T> Fail(string error) =>
            new ApiResult<T> { Success = false, Errors = { error } };

        public static ApiResult<T> Partial(T data, IEnumerable<string> warnings) =>
            new ApiResult<T>
            {
                Success = true,
                Data = data,
                Warnings = warnings.ToList(),
            };
    }

    public class ApiResult
    {
        public bool Success { get; set; }
        // Data carries a payload for commands that have something to report back
        // (the designate counts); null is left out of the JSON entirely.
        public object Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        // Tick: see ApiResult<T>.Tick (fox ticket #4113).
        public int Tick { get; set; }

        public static ApiResult Ok() => new ApiResult { Success = true };

        public static ApiResult Ok(object data) => new ApiResult { Success = true, Data = data };

        public static ApiResult Fail(string error) =>
            new ApiResult { Success = false, Errors = { error } };

        public static ApiResult Unimplemented() =>
            new ApiResult { Success = false, Errors = { "Not implemented" } };

        public static ApiResult Partial(IEnumerable<string> warnings) =>
            new ApiResult { Success = true, Warnings = warnings.ToList() };
    }

    public static class ResponseBuilder
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCaseContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        // Legacy methods for backward compatibility
        public static async Task Success(HttpListenerResponse response, object data)
        {
            await WriteResponse(response, HttpStatusCode.OK, data);
        }

        public static async Task Error(
            HttpListenerResponse response,
            HttpStatusCode statusCode,
            string message
        )
        {
            LogApi.Warning($"API Error {statusCode}: {message}");
            await WriteResponse(response, statusCode, new { error = message });
        }

        // New methods with ApiResult
        public static async Task SendApiResult<T>(
            HttpListenerResponse response,
            ApiResult<T> result
        )
        {
            result.Tick = AnswerTick();
            var statusCode = DetermineStatusCode(result);
            await WriteResponse(response, statusCode, result);
        }

        public static async Task SendApiResult(HttpListenerResponse response, ApiResult result)
        {
            result.Tick = AnswerTick();
            var statusCode = DetermineStatusCode(result);
            await WriteResponse(response, statusCode, result);
        }

        // AnswerTick is the game tick every answer is stamped with (fox ticket
        // #4113): the state the payload describes, read once, here, so no
        // handler can forget it. 0 on the title screen and while a world
        // generates, which the fox reads as "not stamped" rather than as tick
        // zero. An answer served from a cache must carry the tick it was
        // cached at (CacheEntry.GameTickAdded), not this one; no read path
        // caches today, so the stamp is the live tick.
        private static int AnswerTick()
        {
            if (Current.Game == null) return 0;
            TickManager ticks = Find.TickManager;
            return ticks != null ? ticks.TicksGame : 0;
        }

        // Convenience methods for common scenarios
        public static async Task SendSuccess<T>(HttpListenerResponse response, T data)
        {
            await SendApiResult(response, ApiResult<T>.Ok(data));
        }

        public static async Task SendSuccess(HttpListenerResponse response)
        {
            await SendApiResult(response, ApiResult.Ok());
        }

        public static async Task SendError(
            HttpListenerResponse response,
            HttpStatusCode statusCode,
            string errorMessage
        )
        {
            LogApi.Warning($"API Error {statusCode}: {errorMessage}");
            // The caller's status is the answer. Re-deriving it from the words
            // turned an explicit 400 or 500 into a 404 whenever the message
            // said "not found", so a missing parameter arrived as a missing
            // route (fox ticket #3769).
            await WriteResponse(response, statusCode, ApiResult.Fail(errorMessage));
        }

        public static async Task SendValidationError(
            HttpListenerResponse response,
            IEnumerable<string> errors
        )
        {
            var result = new ApiResult { Success = false, Errors = errors.ToList() };
            await SendApiResult(response, result);
        }

        public static async Task SendPartialSuccess<T>(
            HttpListenerResponse response,
            T data,
            IEnumerable<string> warnings
        )
        {
            var result = ApiResult<T>.Partial(data, warnings);
            await SendApiResult(response, result);
        }

        private static HttpStatusCode DetermineStatusCode<T>(ApiResult<T> result)
        {
            if (result.Success)
            {
                if (result.Warnings?.Any() == true)
                    return HttpStatusCode.OK; // 200 with warnings
                return HttpStatusCode.OK; // 200
            }
            else
            {
                if (result.Errors?.Any(e => e.Contains("not found")) == true)
                    return HttpStatusCode.NotFound; // 404
                if (result.Errors?.Any(e => e.Contains("unauthorized")) == true)
                    return HttpStatusCode.Unauthorized; // 401
                if (result.Errors?.Any(e => e.Contains("validation")) == true)
                    return HttpStatusCode.BadRequest; // 400

                return HttpStatusCode.InternalServerError; // 500
            }
        }

        private static HttpStatusCode DetermineStatusCode(ApiResult result)
        {
            if (result.Success)
            {
                if (result.Warnings?.Any() == true)
                    return HttpStatusCode.OK;
                return HttpStatusCode.OK;
            }
            else
            {
                if (result.Errors?.Any(e => e.Contains("not found")) == true)
                    return HttpStatusCode.NotFound;
                if (result.Errors?.Any(e => e.Contains("unauthorized")) == true)
                    return HttpStatusCode.Unauthorized;
                if (result.Errors?.Any(e => e.Contains("validation")) == true)
                    return HttpStatusCode.BadRequest;

                return HttpStatusCode.InternalServerError;
            }
        }

        private static async Task WriteResponse(
            HttpListenerResponse response,
            HttpStatusCode statusCode,
            object data
        )
        {
            try
            {
                // Ensure the response status code is set correctly
                response.StatusCode = (int)statusCode;

                // Set Content-Type header for JSON responses
                response.ContentType = "application/json; charset=utf-8";

                // Serialize the data to a JSON string
                var json = JsonConvert.SerializeObject(data, _jsonSettings);

                // Log the response for debugging (truncate long responses)
                var logJson = json.Length > 1000 ? json.Substring(0, 1000) + "..." : json;
                LogApi.Message($"Response [{statusCode}]: {logJson}", LoggingLevels.DEBUG);

                // Convert the JSON string to bytes
                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                // Write the JSON data to the response output stream
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();

                // Log success for debugging purposes
                LogApi.Info($"Response sent - Status: {statusCode}, Length: {buffer.Length} bytes");
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error writing response: {ex}");
                try
                {
                    response.Abort();
                }
                catch
                {
                    // Ignore any errors on response abort
                }
            }
        }

        // Helper method for sending raw JSON (for SSE or special cases)
        public static async Task SendRawJson(
            HttpListenerResponse response,
            string json,
            HttpStatusCode statusCode = HttpStatusCode.OK
        )
        {
            try
            {
                response.StatusCode = (int)statusCode;
                response.ContentType = "application/json; charset=utf-8";

                var buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error sending raw JSON: {ex}");
                try
                {
                    response.Abort();
                }
                catch { }
            }
        }

        // Helper method for sending plain text
        public static async Task SendPlainText(
            HttpListenerResponse response,
            string text,
            HttpStatusCode statusCode = HttpStatusCode.OK
        )
        {
            try
            {
                response.StatusCode = (int)statusCode;
                response.ContentType = "text/plain; charset=utf-8";

                var buffer = Encoding.UTF8.GetBytes(text);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error sending plain text: {ex}");
                try
                {
                    response.Abort();
                }
                catch { }
            }
        }
    }

    // Extension methods for easier response building in controllers
    public static class ResponseBuilderExtensions
    {
        public static async Task SendJsonResponse<T>(
            this HttpListenerContext context,
            ApiResult<T> result
        )
        {
            // Apply generic filtering if success and query parameters are present
            if (result.Success && result.Data != null && context.Request.QueryString.Count > 0)
            {
                try
                {
                    result.Data = (T)QueryFilter.ApplyToResult(result.Data, context.Request.QueryString);
                }
                catch (Exception ex)
                {
                    LogApi.Warning($"Failed to apply query filters: {ex.Message}");
                    result.Warnings.Add($"Filtering failed: {ex.Message}");
                }
            }
            await ResponseBuilder.SendApiResult(context.Response, result);
        }

        public static async Task SendJsonResponse(
            this HttpListenerContext context,
            ApiResult result
        )
        {
            await ResponseBuilder.SendApiResult(context.Response, result);
        }

        public static async Task SendSuccess<T>(this HttpListenerContext context, T data)
        {
            await ResponseBuilder.SendSuccess(context.Response, data);
        }

        public static async Task SendSuccess(this HttpListenerContext context)
        {
            await ResponseBuilder.SendSuccess(context.Response);
        }

        public static async Task SendError(
            this HttpListenerContext context,
            HttpStatusCode statusCode,
            string errorMessage
        )
        {
            await ResponseBuilder.SendError(context.Response, statusCode, errorMessage);
        }

        public static async Task SendValidationError(
            this HttpListenerContext context,
            IEnumerable<string> errors
        )
        {
            await ResponseBuilder.SendValidationError(context.Response, errors);
        }

        public static async Task SendPartialSuccess<T>(
            this HttpListenerContext context,
            T data,
            IEnumerable<string> warnings
        )
        {
            await ResponseBuilder.SendPartialSuccess(context.Response, data, warnings);
        }
    }
}
