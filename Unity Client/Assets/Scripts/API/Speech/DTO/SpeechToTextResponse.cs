using System;

[Serializable]
public class SpeechToTextResponse
{
    public string filename;
    public string content_type;
    public string provider;
    public string transcription;
}