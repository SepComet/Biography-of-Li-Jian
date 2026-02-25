using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public sealed class OpenAIHttpNetworkService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public OpenAIHttpNetworkService()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler);
        }

        public async Task<OpenAIHeartbeatResult> SendHeartbeatAsync(string url, string apiKey, int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return OpenAIHeartbeatResult.CreateFailure(false, false, 0, "Heartbeat failed. URL is empty.");
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplyAuthorization(request, apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (CancellationTokenSource timeoutCts = new CancellationTokenSource(
                           TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds))))
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    {
                        try
                        {
                            using (HttpResponseMessage response = await _httpClient
                                       .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token)
                                       .ConfigureAwait(false))
                            {
                                long statusCode = (long)response.StatusCode;
                                bool isHttpSuccess = response.IsSuccessStatusCode;
                                if (isHttpSuccess)
                                {
                                    return OpenAIHeartbeatResult.CreateSuccess(statusCode);
                                }

                                if (statusCode == 401 || statusCode == 403)
                                {
                                    return OpenAIHeartbeatResult.CreateFailure(true, false, statusCode,
                                        "Authentication failed. API key may be invalid.");
                                }

                                string errorBody = await TryReadContentAsync(response.Content).ConfigureAwait(false);
                                string errorMessage = string.IsNullOrEmpty(errorBody)
                                    ? string.Format("Heartbeat failed with HTTP {0}.", statusCode.ToString())
                                    : string.Format("Heartbeat failed with HTTP {0}. body={1}", statusCode.ToString(),
                                        errorBody);

                                return OpenAIHeartbeatResult.CreateFailure(true, false, statusCode, errorMessage);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return OpenAIHeartbeatResult.CreateFailure(false, false, 0,
                                    "Heartbeat canceled by caller.");
                            }

                            return OpenAIHeartbeatResult.CreateFailure(false, false, 0, "Heartbeat timeout.");
                        }
                        catch (HttpRequestException exception)
                        {
                            return OpenAIHeartbeatResult.CreateFailure(false, false, 0, exception.Message);
                        }
                        catch (Exception exception)
                        {
                            return OpenAIHeartbeatResult.CreateFailure(false, false, 0, exception.Message);
                        }
                    }
                }
            }
        }

        public async Task<OpenAIStreamResult> StreamChatAsync(
            string url,
            string apiKey,
            string requestBodyJson,
            int timeoutSeconds,
            Action<string> onSseDataPayload,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return OpenAIStreamResult.CreateFailure(false, false, 0, "AI chat request failed. URL is empty.",
                    string.Empty);
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplyAuthorization(request, apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                request.Content = new StringContent(requestBodyJson ?? string.Empty, Encoding.UTF8, "application/json");

                using (CancellationTokenSource timeoutCts = new CancellationTokenSource(
                           TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds))))
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                    {
                        try
                        {
                            using (HttpResponseMessage response = await _httpClient
                                       .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token)
                                       .ConfigureAwait(false))
                            {
                                long statusCode = (long)response.StatusCode;
                                if (!response.IsSuccessStatusCode)
                                {
                                    string errorBody = await TryReadContentAsync(response.Content).ConfigureAwait(false);
                                    string errorMessage = string.Format("HTTP {0} {1}", statusCode.ToString(),
                                        response.ReasonPhrase ?? string.Empty);
                                    return OpenAIStreamResult.CreateFailure(true, false, statusCode, errorMessage,
                                        errorBody);
                                }

                                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    while (true)
                                    {
                                        string line =
                                            await WaitReadLineAsync(reader, linkedCts.Token).ConfigureAwait(false);
                                        if (line == null)
                                        {
                                            break;
                                        }

                                        if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                                        {
                                            continue;
                                        }

                                        string payload = line.Substring(5).Trim();
                                        if (string.IsNullOrEmpty(payload))
                                        {
                                            continue;
                                        }

                                        onSseDataPayload?.Invoke(payload);
                                    }
                                }

                                return OpenAIStreamResult.CreateSuccess(statusCode);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return OpenAIStreamResult.CreateCanceled();
                        }
                        catch (HttpRequestException exception)
                        {
                            return OpenAIStreamResult.CreateFailure(false, false, 0, exception.Message, string.Empty);
                        }
                        catch (Exception exception)
                        {
                            return OpenAIStreamResult.CreateFailure(false, false, 0, exception.Message, string.Empty);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static void ApplyAuthorization(HttpRequestMessage request, string apiKey)
        {
            if (request == null || string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        private static async Task<string> WaitReadLineAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            Task<string> readLineTask = reader.ReadLineAsync();
            if (readLineTask.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                return await readLineTask.ConfigureAwait(false);
            }

            TaskCompletionSource<bool> cancellationTaskSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancellationTaskSource.TrySetResult(true)))
            {
                Task completedTask = await Task.WhenAny(readLineTask, cancellationTaskSource.Task).ConfigureAwait(false);
                if (completedTask != readLineTask)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await readLineTask.ConfigureAwait(false);
        }

        private static async Task<string> TryReadContentAsync(HttpContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            try
            {
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }

    public sealed class OpenAIHeartbeatResult
    {
        public bool IsEndpointReachable { get; private set; }
        public bool IsConnectionValid { get; private set; }
        public long StatusCode { get; private set; }
        public string ErrorMessage { get; private set; }

        public static OpenAIHeartbeatResult CreateSuccess(long statusCode)
        {
            return new OpenAIHeartbeatResult
            {
                IsEndpointReachable = true,
                IsConnectionValid = true,
                StatusCode = statusCode,
                ErrorMessage = string.Empty
            };
        }

        public static OpenAIHeartbeatResult CreateFailure(bool endpointReachable, bool connectionValid, long statusCode,
            string errorMessage)
        {
            return new OpenAIHeartbeatResult
            {
                IsEndpointReachable = endpointReachable,
                IsConnectionValid = connectionValid,
                StatusCode = statusCode,
                ErrorMessage = errorMessage ?? string.Empty
            };
        }
    }

    public sealed class OpenAIStreamResult
    {
        public bool IsSuccess { get; private set; }
        public bool IsCanceled { get; private set; }
        public bool IsEndpointReachable { get; private set; }
        public bool IsConnectionValid { get; private set; }
        public long StatusCode { get; private set; }
        public string ErrorMessage { get; private set; }
        public string ErrorBody { get; private set; }

        public static OpenAIStreamResult CreateSuccess(long statusCode)
        {
            return new OpenAIStreamResult
            {
                IsSuccess = true,
                IsCanceled = false,
                IsEndpointReachable = true,
                IsConnectionValid = true,
                StatusCode = statusCode,
                ErrorMessage = string.Empty,
                ErrorBody = string.Empty
            };
        }

        public static OpenAIStreamResult CreateCanceled()
        {
            return new OpenAIStreamResult
            {
                IsSuccess = false,
                IsCanceled = true,
                IsEndpointReachable = false,
                IsConnectionValid = false,
                StatusCode = 0,
                ErrorMessage = "Request canceled.",
                ErrorBody = string.Empty
            };
        }

        public static OpenAIStreamResult CreateFailure(bool endpointReachable, bool connectionValid, long statusCode,
            string errorMessage, string errorBody)
        {
            return new OpenAIStreamResult
            {
                IsSuccess = false,
                IsCanceled = false,
                IsEndpointReachable = endpointReachable,
                IsConnectionValid = connectionValid,
                StatusCode = statusCode,
                ErrorMessage = errorMessage ?? string.Empty,
                ErrorBody = errorBody ?? string.Empty
            };
        }
    }
}
