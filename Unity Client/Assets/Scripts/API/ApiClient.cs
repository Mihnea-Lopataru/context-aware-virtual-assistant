using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;

public enum ApiServiceType
{
    Backend,
    Speech
}

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance;

    [Header("API Config")]
    [SerializeField] private string backendBaseUrl = "http://localhost:8000";
    [SerializeField] private string speechBaseUrl = "http://localhost:8001";
    [SerializeField] private int requestTimeoutSeconds = 120;
    [SerializeField] private bool logSuccessfulRequests = true;

    private Dictionary<ApiServiceType, string> baseUrls;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            baseUrls = new Dictionary<ApiServiceType, string>
            {
                { ApiServiceType.Backend, NormalizeBaseUrl(backendBaseUrl) },
                { ApiServiceType.Speech, NormalizeBaseUrl(speechBaseUrl) }
            };

            Debug.Log(
                $"[ApiClient] Initialized. Backend={baseUrls[ApiServiceType.Backend]}, Speech={baseUrls[ApiServiceType.Speech]}, Timeout={requestTimeoutSeconds}s");
        }
        else
        {
            Debug.Log("[ApiClient] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public string GetBaseUrl(ApiServiceType service)
    {
        if (baseUrls == null)
            throw new Exception("ApiClient not initialized.");

        if (!baseUrls.TryGetValue(service, out var url) || string.IsNullOrWhiteSpace(url))
            throw new Exception($"No base URL configured for service: {service}");

        return url;
    }

    public Task<T> Get<T>(string endpoint, ApiServiceType service = ApiServiceType.Backend)
    {
        return SendRequest<T>(endpoint, "GET", null, service);
    }

    public Task<T> Post<T>(string endpoint, object body, ApiServiceType service = ApiServiceType.Backend)
    {
        return SendRequest<T>(endpoint, "POST", body, service);
    }

    public Task<T> Patch<T>(string endpoint, object body, ApiServiceType service = ApiServiceType.Backend)
    {
        return SendRequest<T>(endpoint, "PATCH", body, service);
    }

    public Task<T> Delete<T>(string endpoint, ApiServiceType service = ApiServiceType.Backend)
    {
        return SendRequest<T>(endpoint, "DELETE", null, service);
    }

    private async Task<T> SendRequest<T>(
        string endpoint,
        string method,
        object body,
        ApiServiceType service)
    {
        string baseUrl = GetBaseUrl(service);
        string url = $"{baseUrl}{endpoint}";

        string json = body != null ? JsonConvert.SerializeObject(body) : null;

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.timeout = requestTimeoutSeconds;

            if (body != null)
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(jsonBytes);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            await SendAsync(request);

            string responseText = request.downloadHandler.text;

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[ApiClient] JSON parse failed for {method} {url}. Response preview: {Preview(responseText)}");
                Debug.LogException(e);
                throw;
            }
        }
    }

    private async Task SendAsync(UnityWebRequest request)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        stopwatch.Stop();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error =
                $"[ApiClient] HTTP request failed: {request.method} {request.url}\n" +
                $"Result: {request.result}\n" +
                $"Code: {request.responseCode}\n" +
                $"Error: {request.error}\n" +
                $"Elapsed: {stopwatch.ElapsedMilliseconds}ms\n" +
                $"Response: {Preview(request.downloadHandler?.text)}";

            Debug.LogError(error);
            throw new Exception(error);
        }

        if (logSuccessfulRequests)
        {
            Debug.Log(
                $"[ApiClient] {request.method} {request.url} -> {request.responseCode} in {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return string.Empty;

        return baseUrl.Trim().TrimEnd('/');
    }

    private static string Preview(string value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value))
            return "<empty>";

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
