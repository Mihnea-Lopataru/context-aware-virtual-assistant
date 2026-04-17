using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections.Generic;

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

    private Dictionary<ApiServiceType, string> baseUrls;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            baseUrls = new Dictionary<ApiServiceType, string>
            {
                { ApiServiceType.Backend, backendBaseUrl },
                { ApiServiceType.Speech, speechBaseUrl }
            };
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetBaseUrl(ApiServiceType service)
    {
        if (baseUrls == null)
            throw new Exception("ApiClient not initialized.");

        if (!baseUrls.TryGetValue(service, out var url))
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
        string baseUrl = baseUrls[service];
        string url = $"{baseUrl}{endpoint}";

        string json = body != null ? JsonConvert.SerializeObject(body) : null;

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
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
                Debug.LogError($"JSON Parse Error: {e.Message}");
                throw;
            }
        }
    }

    private async Task SendAsync(UnityWebRequest request)
    {
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error = $"HTTP ERROR: {request.error}\n" +
                           $"Code: {request.responseCode}\n" +
                           $"Response: {request.downloadHandler?.text}";

            Debug.LogError(error);
            throw new Exception(error);
        }
    }
}