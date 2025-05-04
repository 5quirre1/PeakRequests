/*   /////////////////////////////////////////////////////////////////////////////
 *   
 *   PeakRequests - Made by 5quirre1 :3
 *   
 *   i hate how you do requests in c# because i mainly code in python. python has
 *   SUPER simple syntax for doing requests (mostly because python is SUPER easy)
 *   and i want to keep that same lazy feeling when doing requests in c#. 
 *  
 *   so uh it's based off the python requests module!!! also i'm still KINDA new
 *   at c# so don't expect it to be godly LMFAO so yea peak
 *   
 *   /////////////////////////////////////////////////////////////////////////////
 *   ------------------------------------------------------------------------------
 *   MIT License
 *
 *   Copyright (c) 2025 Squirrel
 *
 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 *
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 *
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 *   ------------------------------------------------------------------------------
 *   
 */

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PeakRequests
{
    public class PeakResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public Dictionary<string, IEnumerable<string>> Headers { get; set; } =
            new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        public string? Content { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }

        public PeakResponse() { }

        public PeakResponse(HttpResponseMessage response, string content)
        {
            StatusCode = response.StatusCode;
            IsSuccessful = response.IsSuccessStatusCode;
            ErrorMessage = IsSuccessful
                ? null
                : $"HTTP error: {(int)StatusCode} - {response.ReasonPhrase}";
            Content = content;

            Headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers)
            {
                Headers[header.Key] = header.Value;
            }
            if (response.Content?.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    if (Headers.ContainsKey(header.Key))
                        Headers[header.Key] = header.Value.Union(Headers[header.Key]);
                    else
                        Headers[header.Key] = header.Value;
                }
            }
        }

        public JsonNode? Json()
        {
            if (string.IsNullOrEmpty(Content))
                return null;

            try
            {
                return JsonNode.Parse(Content);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"failed to parse json: {ex.Message}", ex);
            }
        }
    }

    public static class PeakRequests
    {
        private static readonly Lazy<HttpClient> _client = new Lazy<HttpClient>(
            () =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 5,
                    UseCookies = false
                };

                System.Net.ServicePointManager.ServerCertificateValidationCallback = (
                    sender,
                    cert,
                    chain,
                    sslPolicyErrors
                ) => true;

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.Timeout = TimeSpan.FromSeconds(100);
                return client;
            }
        );

        private static HttpClient Client => _client.Value;

        public static JsonNode? ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonNode.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"failed to parse json: {ex.Message}", ex);
            }
        }

        private static HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            HttpContent? content,
            Dictionary<string, string>? headers
        )
        {
            var request = new HttpRequestMessage(method, url);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (content != null)
            {
                request.Content = content;
            }
            return request;
        }
        private static async Task<PeakResponse> SendRequest(
            HttpMethod method,
            string url,
            HttpContent? content = null,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            try
            {
                using (
                    var cts = new CancellationTokenSource(
                        timeoutSeconds > 0
                          ? TimeSpan.FromSeconds(timeoutSeconds)
                          : Timeout.InfiniteTimeSpan
                    )
                )
                using (var request = CreateRequest(method, url, content, headers))
                {
                    using (
                        HttpResponseMessage response = await Client.SendAsync(request, cts.Token)
                    )
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        var peakResponse = new PeakResponse(response, responseContent);

                        if (throwOnError)
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        return peakResponse;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex?.CancellationToken == null || !ex.CancellationToken.IsCancellationRequested)
                {
                    return new PeakResponse
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"request to {url} timed out after {timeoutSeconds} seconds"
                    };
                }
                else
                {
                    return new PeakResponse
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"request to {url} was cancelled"
                    };
                }
            }
            catch (Exception ex)
            {
                if (throwOnError)
                {
                    throw;
                }

                return new PeakResponse { IsSuccessful = false, ErrorMessage = ex.Message };
            }
        }

        public static async Task<PeakResponse> Get(
            string url,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        ) => await SendRequest(HttpMethod.Get, url, null, headers, timeoutSeconds, throwOnError);

        public static async Task<PeakResponse> Post(
            string url,
            Dictionary<string, string>? data = null,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            HttpContent? content = data != null ? new FormUrlEncodedContent(data) : null;
            return await SendRequest(
                HttpMethod.Post,
                url,
                content,
                headers,
                timeoutSeconds,
                throwOnError
            );
        }

        public static async Task<PeakResponse> Post(
            string url,
            string? jsonData = null,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            HttpContent? content =
                jsonData != null
                    ? new StringContent(jsonData, Encoding.UTF8, "application/json")
                    : null;
            return await SendRequest(
                HttpMethod.Post,
                url,
                content,
                headers,
                timeoutSeconds,
                throwOnError
            );
        }

        public static async Task<PeakResponse> Put(
            string url,
            string? jsonData = null,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            HttpContent? content =
                jsonData != null
                    ? new StringContent(jsonData, Encoding.UTF8, "application/json")
                    : null;
            return await SendRequest(
                HttpMethod.Put,
                url,
                content,
                headers,
                timeoutSeconds,
                throwOnError
            );
        }

        public static async Task<PeakResponse> Delete(
            string url,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        ) => await SendRequest(HttpMethod.Delete, url, null, headers, timeoutSeconds, throwOnError);

        public static async Task<PeakResponse> Patch(
            string url,
            string? jsonData = null,
            Dictionary<string, string>? headers = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            HttpContent? content =
                jsonData != null
                    ? new StringContent(jsonData, Encoding.UTF8, "application/json")
                    : null;
            return await SendRequest(
                new HttpMethod("PATCH"),
                url,
                content,
                headers,
                timeoutSeconds,
                throwOnError
            );
        }

        public static async Task<PeakResponse> Request(
            string method,
            string url,
            Dictionary<string, string>? headers = null,
            Dictionary<string, string>? data = null,
            string? jsonData = null,
            int timeoutSeconds = 100,
            bool throwOnError = false
        )
        {
            HttpMethod httpMethod;
            switch (method.ToUpper())
            {
                case "GET":
                    httpMethod = HttpMethod.Get;
                    break;
                case "POST":
                    httpMethod = HttpMethod.Post;
                    break;
                case "PUT":
                    httpMethod = HttpMethod.Put;
                    break;
                case "DELETE":
                    httpMethod = HttpMethod.Delete;
                    break;
                case "PATCH":
                    httpMethod = HttpMethod.Patch;
                    break;
                default:
                    throw new ArgumentException($"unsupported http method: {method}");
            }

            HttpContent? content = null;
            if (data != null)
            {
                content = new FormUrlEncodedContent(data);
            }
            else if (jsonData != null)
            {
                content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            }

            return await SendRequest(
                httpMethod,
                url,
                content,
                headers,
                timeoutSeconds,
                throwOnError
            );
        }
    }
}
