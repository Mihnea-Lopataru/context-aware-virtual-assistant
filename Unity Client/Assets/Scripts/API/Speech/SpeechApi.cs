using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Stopwatch = System.Diagnostics.Stopwatch;

public class SpeechApi
{
    private const string STT_ENDPOINT = "/speech-to-text";
    private const string TTS_ENDPOINT = "/text-to-speech";

    public async Task<SpeechToTextResponse> SpeechToText(byte[] audioBytes)
    {
        if (audioBytes == null || audioBytes.Length == 0)
            throw new ArgumentException("Audio payload is empty.", nameof(audioBytes));

        string provider = SpeechConfig.Instance != null
            ? SpeechConfig.Instance.GetSTTProviderString()
            : "vosk";

        string endpoint = $"{STT_ENDPOINT}?provider={provider}";
        string url = ApiClient.Instance.GetBaseUrl(ApiServiceType.Speech) + endpoint;

        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "file",
            audioBytes,
            "audio.wav",
            "audio/wav"
        );

        Debug.Log($"[SpeechApi] STT request started. Provider={provider}, Bytes={audioBytes.Length}");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            await SendAsync(request);

            string responseText = request.downloadHandler.text;

            try
            {
                var response = JsonConvert.DeserializeObject<SpeechToTextResponse>(responseText);
                Debug.Log($"[SpeechApi] STT response received. HasTranscription={!string.IsNullOrWhiteSpace(response?.transcription)}");
                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpeechApi] Failed to parse STT response. Response preview: {Preview(responseText)}");
                Debug.LogException(e);
                throw;
            }
        }
    }

    public async Task<AudioClip> TextToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text is empty.");

        string provider = SpeechConfig.Instance != null
            ? SpeechConfig.Instance.GetTTSProviderString()
            : "piper";

        string endpoint = $"{TTS_ENDPOINT}?text={UnityWebRequest.EscapeURL(text)}&provider={provider}";
        string url = $"{ApiClient.Instance.GetBaseUrl(ApiServiceType.Speech)}{endpoint}";

        Debug.Log($"[SpeechApi] TTS request started. Provider={provider}, TextLength={text.Length}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);

            await SendAsync(request);

            AudioClip clip = ((DownloadHandlerAudioClip)request.downloadHandler).audioClip;

            if (clip == null)
                throw new Exception("Failed to decode AudioClip.");

            Debug.Log($"[SpeechApi] TTS audio decoded. ClipLength={clip.length:0.00}s, Frequency={clip.frequency}, Channels={clip.channels}");

            return clip;
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
                $"[SpeechApi] HTTP request failed: {request.method} {request.url}\n" +
                $"Result: {request.result}\n" +
                $"Code: {request.responseCode}\n" +
                $"Error: {request.error}\n" +
                $"Elapsed: {stopwatch.ElapsedMilliseconds}ms\n" +
                $"Response: {Preview(request.downloadHandler?.text)}";

            Debug.LogError(error);
            throw new Exception(error);
        }

        Debug.Log($"[SpeechApi] {request.method} {request.url} -> {request.responseCode} in {stopwatch.ElapsedMilliseconds}ms");
    }

    private static string Preview(string value, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(value))
            return "<empty>";

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
