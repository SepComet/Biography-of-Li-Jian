using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Definition.DataStruct;
using GameFramework;
using Network;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    [DisallowMultipleComponent]
    public class AIChatComponent : GameFrameworkComponent
    {
        [Header("OpenAI Config")]
        [SerializeField] private string _configFileName = "openai_config.json";

        [SerializeField] private string _chatCompletionsPath = "/chat/completions";
        [SerializeField] private string _heartbeatPath = "/models";

        private string _apiBaseUrl;
        private string _apiKey;
        private string _model;

        [Header("Request Options")] [SerializeField]
        private bool _autoInitializeOnEnable = true;

        [SerializeField] private int _chatTimeoutSeconds = 60;
        [SerializeField] private float _temperature = 0.7f;
        [SerializeField] [TextArea(2, 6)] private string _systemPrompt = string.Empty;

        [Header("Heartbeat")] [SerializeField] private float _heartbeatIntervalSeconds = 5f;
        [SerializeField] private int _heartbeatTimeoutSeconds = 8;

        private readonly List<OpenAIStreamChunk> _chunkCache = new List<OpenAIStreamChunk>();
        private readonly StringBuilder _assistantResponseCache = new StringBuilder();
        private readonly Queue<string> _pendingSsePayloads = new Queue<string>();
        private readonly object _streamQueueLock = new object();
        private readonly OpenAIHttpNetworkService _networkService = new OpenAIHttpNetworkService();

        private Coroutine _heartbeatCoroutine;
        private Coroutine _chatCoroutine;
        private CancellationTokenSource _heartbeatRequestCancellation;
        private CancellationTokenSource _chatRequestCancellation;
        private bool _isInitialized;
        private bool _isHeartbeatRequesting;
        private bool _streamDoneReceived;

        public bool IsInitialized => _isInitialized;
        public bool IsRequesting => _chatCoroutine != null;
        public bool IsEndpointReachable { get; private set; }
        public bool IsConnectionValid { get; private set; }
        public long LastHeartbeatStatusCode { get; private set; }
        public string LastHeartbeatErrorMessage { get; private set; } = string.Empty;
        public string LastRequestErrorMessage { get; private set; } = string.Empty;
        public string CachedResponseText => _assistantResponseCache.ToString();
        public IReadOnlyList<OpenAIStreamChunk> CachedChunks => _chunkCache;

        public event Action<OpenAIStreamChunk> StreamChunkReceived;
        public event Action<string> StreamTextUpdated;
        public event Action<string> StreamRequestCompleted;
        public event Action<string> StreamRequestFailed;

        private void OnEnable()
        {
            if (_autoInitializeOnEnable)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            StopHeartbeat();
            CancelCurrentRequest();
        }

        private void OnDestroy()
        {
            StopHeartbeat();
            CancelCurrentRequest();
            _networkService.Dispose();
        }

        public void Initialize()
        {
            if (!LoadConfigFromStreamingAssets())
            {
                _isInitialized = false;
                return;
            }

            _isInitialized = true;
            StartHeartbeat();
        }

        public void StartHeartbeat()
        {
            if (_heartbeatCoroutine != null)
            {
                return;
            }

            _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        }

        public void StopHeartbeat()
        {
            if (_heartbeatRequestCancellation != null && !_heartbeatRequestCancellation.IsCancellationRequested)
            {
                _heartbeatRequestCancellation.Cancel();
            }

            if (_heartbeatCoroutine == null)
            {
                return;
            }

            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        public void TriggerHeartbeat()
        {
            StartCoroutine(SendHeartbeatOnce());
        }

        public bool SendChat(string userInput)
        {
            return SendChat(userInput, string.Empty);
        }

        public bool SendChat(string userInput, string extraSystemPrompt)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                LastRequestErrorMessage = "AI chat request failed. userInput is empty.";
                StreamRequestFailed?.Invoke(LastRequestErrorMessage);
                Log.Warning(LastRequestErrorMessage);
                return false;
            }

            if (_chatCoroutine != null)
            {
                LastRequestErrorMessage = "AI chat request failed. Previous request is still running.";
                StreamRequestFailed?.Invoke(LastRequestErrorMessage);
                Log.Warning(LastRequestErrorMessage);
                return false;
            }

            _chatCoroutine = StartCoroutine(SendChatCoroutine(userInput, extraSystemPrompt));
            return true;
        }

        public void CancelCurrentRequest()
        {
            if (_chatRequestCancellation != null && !_chatRequestCancellation.IsCancellationRequested)
            {
                _chatRequestCancellation.Cancel();
            }
        }

        public void ClearChatCache()
        {
            _chunkCache.Clear();
            _assistantResponseCache.Length = 0;
            _streamDoneReceived = false;

            lock (_streamQueueLock)
            {
                _pendingSsePayloads.Clear();
            }
        }

        public string BuildOpenAIRequestBody(string userInput)
        {
            return BuildOpenAIRequestBody(userInput, string.Empty);
        }

        public string BuildOpenAIRequestBody(string userInput, string extraSystemPrompt)
        {
            OpenAIChatRequest requestBody = BuildRequestData(userInput, extraSystemPrompt);
            return Utility.Json.ToJson(requestBody);
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                yield return SendHeartbeatOnce();
                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, _heartbeatIntervalSeconds));
            }
        }

        private IEnumerator SendHeartbeatOnce()
        {
            if (_isHeartbeatRequesting)
            {
                yield break;
            }

            _isHeartbeatRequesting = true;
            try
            {
                string heartbeatUrl = BuildEndpointUrl(_heartbeatPath);
                if (string.IsNullOrWhiteSpace(heartbeatUrl))
                {
                    UpdateHeartbeatState(false, false, 0, "Heartbeat failed. URL is empty.");
                    yield break;
                }

                if (_heartbeatRequestCancellation != null)
                {
                    _heartbeatRequestCancellation.Dispose();
                    _heartbeatRequestCancellation = null;
                }

                _heartbeatRequestCancellation = new CancellationTokenSource();
                Task<OpenAIHeartbeatResult> heartbeatTask = _networkService.SendHeartbeatAsync(
                    heartbeatUrl,
                    _apiKey,
                    _heartbeatTimeoutSeconds,
                    _heartbeatRequestCancellation.Token);

                yield return WaitTask(heartbeatTask);

                if (heartbeatTask.IsFaulted)
                {
                    UpdateHeartbeatState(false, false, 0, GetTaskErrorMessage(heartbeatTask.Exception));
                }
                else if (heartbeatTask.IsCanceled)
                {
                    UpdateHeartbeatState(false, false, 0, "Heartbeat canceled.");
                }
                else
                {
                    OpenAIHeartbeatResult result = heartbeatTask.Result;
                    UpdateHeartbeatState(result.IsEndpointReachable, result.IsConnectionValid, result.StatusCode,
                        result.ErrorMessage);
                }
            }
            finally
            {
                if (_heartbeatRequestCancellation != null)
                {
                    _heartbeatRequestCancellation.Dispose();
                    _heartbeatRequestCancellation = null;
                }

                _isHeartbeatRequesting = false;
            }
        }

        private IEnumerator SendChatCoroutine(string userInput, string extraSystemPrompt)
        {
            LastRequestErrorMessage = string.Empty;
            ClearChatCache();

            string chatUrl = BuildEndpointUrl(_chatCompletionsPath);
            if (string.IsNullOrWhiteSpace(chatUrl))
            {
                LastRequestErrorMessage = "AI chat request failed. URL is empty.";
                StreamRequestFailed?.Invoke(LastRequestErrorMessage);
                Log.Warning(LastRequestErrorMessage);
                _chatCoroutine = null;
                yield break;
            }

            if (_chatRequestCancellation != null)
            {
                _chatRequestCancellation.Dispose();
                _chatRequestCancellation = null;
            }

            _chatRequestCancellation = new CancellationTokenSource();
            string requestBody = BuildOpenAIRequestBody(userInput, extraSystemPrompt);

            try
            {
                Task<OpenAIStreamResult> requestTask = _networkService.StreamChatAsync(
                    chatUrl,
                    _apiKey,
                    requestBody,
                    _chatTimeoutSeconds,
                    EnqueueSsePayload,
                    _chatRequestCancellation.Token);

                while (!requestTask.IsCompleted)
                {
                    FlushPendingSsePayloads();
                    yield return null;
                }

                FlushPendingSsePayloads();

                if (requestTask.IsFaulted)
                {
                    LastRequestErrorMessage = GetTaskErrorMessage(requestTask.Exception);
                    StreamRequestFailed?.Invoke(LastRequestErrorMessage);
                    Log.Warning("AI chat request failed. {0}", LastRequestErrorMessage);
                    yield break;
                }

                OpenAIStreamResult result = requestTask.Result;
                if (!result.IsSuccess)
                {
                    LastRequestErrorMessage = BuildRequestErrorMessage(result);
                    StreamRequestFailed?.Invoke(LastRequestErrorMessage);
                    Log.Warning("AI chat request failed. {0}", LastRequestErrorMessage);
                    yield break;
                }

                if (!_streamDoneReceived && _chunkCache.Count > 0)
                {
                    Log.Warning("AI chat stream completed without receiving [DONE] marker.");
                }

                StreamRequestCompleted?.Invoke(_assistantResponseCache.ToString());
            }
            finally
            {
                if (_chatRequestCancellation != null)
                {
                    _chatRequestCancellation.Dispose();
                    _chatRequestCancellation = null;
                }

                _chatCoroutine = null;
            }
        }

        private void EnqueueSsePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            lock (_streamQueueLock)
            {
                _pendingSsePayloads.Enqueue(payload);
            }
        }

        private void FlushPendingSsePayloads()
        {
            while (true)
            {
                string payload;
                lock (_streamQueueLock)
                {
                    if (_pendingSsePayloads.Count <= 0)
                    {
                        break;
                    }

                    payload = _pendingSsePayloads.Dequeue();
                }

                ParseSsePayload(payload);
            }
        }

        private void ParseSsePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                _streamDoneReceived = true;
                return;
            }

            OpenAIStreamChunk chunk;
            try
            {
                chunk = Utility.Json.ToObject<OpenAIStreamChunk>(payload);
            }
            catch (Exception exception)
            {
                Log.Warning("AI chat chunk parse failed. reason='{0}'", exception.Message);
                return;
            }

            if (chunk == null)
            {
                return;
            }

            _chunkCache.Add(chunk);
            StreamChunkReceived?.Invoke(chunk);

            string textDelta = ExtractTextDelta(chunk);
            if (string.IsNullOrEmpty(textDelta))
            {
                return;
            }

            _assistantResponseCache.Append(textDelta);
            StreamTextUpdated?.Invoke(textDelta);
        }

        private void UpdateHeartbeatState(bool endpointReachable, bool connectionValid, long statusCode,
            string errorMessage)
        {
            IsEndpointReachable = endpointReachable;
            IsConnectionValid = connectionValid;
            LastHeartbeatStatusCode = statusCode;
            LastHeartbeatErrorMessage = errorMessage ?? string.Empty;
        }

        private OpenAIChatRequest BuildRequestData(string userInput, string extraSystemPrompt)
        {
            OpenAIChatRequest request = new OpenAIChatRequest
            {
                model = _model,
                stream = true,
                temperature = (double)Mathf.Clamp(_temperature, 0f, 2f),
                messages = new List<OpenAIMessage>()
            };

            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                request.messages.Add(new OpenAIMessage
                {
                    role = "system",
                    content = _systemPrompt
                });
            }

            if (!string.IsNullOrWhiteSpace(extraSystemPrompt))
            {
                request.messages.Add(new OpenAIMessage
                {
                    role = "system",
                    content = extraSystemPrompt
                });
            }

            request.messages.Add(new OpenAIMessage
            {
                role = "user",
                content = userInput
            });

            return request;
        }

        private string BuildEndpointUrl(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                return string.Empty;
            }

            if (pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return pathOrUrl.Trim();
            }

            if (string.IsNullOrWhiteSpace(_apiBaseUrl))
            {
                return string.Empty;
            }

            return Utility.Text.Format("{0}/{1}", _apiBaseUrl.TrimEnd('/'), pathOrUrl.TrimStart('/'));
        }

        private bool LoadConfigFromStreamingAssets()
        {
            if (string.IsNullOrWhiteSpace(_configFileName))
            {
                Log.Error("AIChat config filename is empty.");
                return false;
            }

            string configPath = Path.Combine(Application.streamingAssetsPath, _configFileName);
            if (!File.Exists(configPath))
            {
                Log.Error("AIChat config file not found. path='{0}'", configPath);
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(configPath, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Log.Error("AIChat config read failed. path='{0}', error='{1}'", configPath, exception.Message);
                return false;
            }

            OpenAIConfig config;
            try
            {
                config = Utility.Json.ToObject<OpenAIConfig>(json);
            }
            catch (Exception exception)
            {
                Log.Error("AIChat config parse failed. error='{0}'", exception.Message);
                return false;
            }

            if (config == null ||
                string.IsNullOrWhiteSpace(config.apiBaseUrl) ||
                string.IsNullOrWhiteSpace(config.apiKey) ||
                string.IsNullOrWhiteSpace(config.model))
            {
                Log.Error("AIChat config is invalid. baseUrl/key/model must be provided.");
                return false;
            }

            _apiBaseUrl = config.apiBaseUrl;
            _apiKey = config.apiKey;
            _model = config.model;
            return true;
        }

        [Serializable]
        private sealed class OpenAIConfig
        {
            public string apiBaseUrl;
            public string apiKey;
            public string model;
        }

        private static IEnumerator WaitTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }

        private static string GetTaskErrorMessage(AggregateException exception)
        {
            if (exception == null)
            {
                return "Unknown task error.";
            }

            Exception baseException = exception.GetBaseException();
            return baseException != null ? baseException.Message : exception.Message;
        }

        private static string BuildRequestErrorMessage(OpenAIStreamResult result)
        {
            if (result == null)
            {
                return "AI chat request failed. Result is null.";
            }

            if (result.IsCanceled)
            {
                return "AI chat request canceled.";
            }

            if (string.IsNullOrEmpty(result.ErrorBody))
            {
                return string.Format("{0} (status={1})", result.ErrorMessage, result.StatusCode.ToString());
            }

            return string.Format("{0} (status={1}) body={2}", result.ErrorMessage, result.StatusCode.ToString(),
                result.ErrorBody);
        }

        private static string ExtractTextDelta(OpenAIStreamChunk chunk)
        {
            if (chunk == null || chunk.choices == null || chunk.choices.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = null;
            foreach (var choice in chunk.choices)
            {
                if (choice == null || choice.delta == null || string.IsNullOrEmpty(choice.delta.content))
                {
                    continue;
                }

                if (builder == null)
                {
                    builder = new StringBuilder();
                }

                builder.Append(choice.delta.content);
            }

            return builder != null ? builder.ToString() : string.Empty;
        }
    }
}
