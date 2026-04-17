using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class SpeechApi
{
    private const string STT_ENDPOINT = "/speech-to-text";
    private const string TTS_ENDPOINT = "/text-to-speech";

    public async Task<SpeechToTextResponse> SpeechToText(byte[] audioBytes)
    {
        string endpoint = "/speech-to-text?provider=" + SpeechConfig.Instance.GetSTTProviderString();

        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "file",
            audioBytes,
            "audio.wav",
            "audio/wav"
        );

        using (UnityWebRequest request = UnityWebRequest.Post(
            ApiClient.Instance.GetBaseUrl(ApiServiceType.Speech) + endpoint,
            form))
        {
            await SendAsync(request);

            string responseText = request.downloadHandler.text;

            return JsonConvert.DeserializeObject<SpeechToTextResponse>(responseText);
        }
    }

    public async Task<AudioClip> TextToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text is empty.");

        string provider = SpeechConfig.Instance.GetTTSProviderString();

        string endpoint = $"/text-to-speech?text={UnityWebRequest.EscapeURL(text)}&provider={provider}";
        string url = $"{ApiClient.Instance.GetBaseUrl(ApiServiceType.Speech)}{endpoint}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);

            await SendAsync(request);

            AudioClip clip = ((DownloadHandlerAudioClip)request.downloadHandler).audioClip;

            if (clip == null)
                throw new Exception("Failed to decode AudioClip.");

            return clip;
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