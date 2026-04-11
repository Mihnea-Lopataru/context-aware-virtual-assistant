using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance;

    [Header("API Config")]
    [SerializeField] private string baseUrl = "http://localhost:8000";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // GET
    // =========================
    public async Task<T> Get<T>(string endpoint)
    {
        string url = $"{baseUrl}{endpoint}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            return await SendRequest<T>(request);
        }
    }

    // =========================
    // POST
    // =========================
    public async Task<T> Post<T>(string endpoint, object body)
    {
        string url = $"{baseUrl}{endpoint}";
        string json = JsonConvert.SerializeObject(body);

        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequest<T>(request);
        }
    }

    // =========================
    // PATCH
    // =========================
    public async Task<T> Patch<T>(string endpoint, object body)
    {
        string url = $"{baseUrl}{endpoint}";
        string json = JsonConvert.SerializeObject(body);

        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            return await SendRequest<T>(request);
        }
    }

    // =========================
    // CORE
    // =========================
    private async Task<T> SendRequest<T>(UnityWebRequest request)
    {
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API Error: {request.error}");
            throw new Exception(request.error);
        }

        string json = request.downloadHandler.text;

        Debug.Log($"Response: {json}");

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON Parse Error: {e.Message}");
            throw;
        }
    }
}