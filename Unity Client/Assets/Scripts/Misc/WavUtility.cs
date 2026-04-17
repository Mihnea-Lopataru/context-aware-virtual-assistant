using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = ConvertToPCM16(samples);

        byte[] wav = AddWavHeader(pcmData, clip.channels, clip.frequency);

        return wav;
    }

    private static byte[] ConvertToPCM16(float[] samples)
    {
        byte[] pcm = new byte[samples.Length * 2];

        int index = 0;

        foreach (var sample in samples)
        {
            short intSample = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);

            pcm[index++] = (byte)(intSample & 0xFF);
            pcm[index++] = (byte)((intSample >> 8) & 0xFF);
        }

        return pcm;
    }

    private static byte[] AddWavHeader(byte[] pcmData, int channels, int sampleRate)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int byteRate = sampleRate * channels * 2;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmData.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return stream.ToArray();
        }
    }
}