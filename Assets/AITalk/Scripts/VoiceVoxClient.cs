using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class VoiceVoxClient : MonoBehaviour
{
    private string voiceVoxHost = "localhost:50021/";

    /// <summary>
    /// 音声合成を実行するキャラクターのID
    /// </summary>
    [SerializeField] int speakerId = 10;

    /// <summary>
    /// 音声合成に必要なクエリ
    /// </summary>
    private string audioQuery = "";

    /// <summary>
    /// 音声合成の完了を通知する
    /// </summary>
    public UnityEvent<AudioClip> OnSynthesisAudio;

    /// <summary>
    /// 音声合成に必要なクエリ文字列を取得する
    /// </summary>
    /// <param name="text">入力文字列</param>
    /// <returns></returns>
    IEnumerator RequestAudioQueryRoutine(string text)
    {
        var queryString = HttpUtility.ParseQueryString("");
        queryString.Add("text", text);
        queryString.Add("speaker", this.speakerId.ToString()); 

        // URIとクエリをマージ
        var uriBuilder = new UriBuilder(voiceVoxHost + "audio_query") {
            Query = queryString.ToString()
        };
        var content = new Dictionary<string,string> () {
        };

        using var req = UnityWebRequest.Post(uriBuilder.Uri, content);
        yield return req.SendWebRequest();

        if(req.result != UnityWebRequest.Result.Success)
        {
            ErrorPopper.PopError("音声クエリの取得に失敗しました\n" + req.downloadHandler.text);
            Debug.LogError(req.downloadHandler.text);
            yield break;
        }

        audioQuery = req.downloadHandler.text;
    }

    /// <summary>
    /// 音声合成を実行する
    /// </summary>
    /// <returns></returns>
    IEnumerator SynthesisAudioRoutine()
    {
        var queryString = HttpUtility.ParseQueryString("");
        queryString.Add("speaker", this.speakerId.ToString()); 

        // URIとクエリをマージ
        var uriBuilder = new UriBuilder(voiceVoxHost + "synthesis") {
            Query = queryString.ToString()
        };
        var content = new Dictionary<string,string> () {
        };
        using var req = UnityWebRequest.Post(uriBuilder.Uri, content);

        // リクエストボディにクエリを設定
        byte[] rawAudioQuery =  Encoding.UTF8.GetBytes(audioQuery);
        req.uploadHandler = new UploadHandlerRaw(rawAudioQuery);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if(req.result != UnityWebRequest.Result.Success)
        {
            ErrorPopper.PopError("音声合成に失敗しました\n" + req.downloadHandler.error);
            Debug.LogError(req.downloadHandler.error);
            yield break;
        }

        // AudioClipを生成
        AudioClip clip = CreateAudioClip(req.downloadHandler.data);
        this.OnSynthesisAudio.Invoke(clip);
    }

    AudioClip CreateAudioClip(byte[] wavBytes, string clipName = "wav")
    {
        int pos = 12;

        int channels = 0;
        int sampleRate = 0;
        int bitsPerSample = 0;
        byte[] audioData = null;

        while (pos < wavBytes.Length)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(wavBytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(wavBytes, pos + 4);
            pos += 8;

            if (chunkId == "fmt ")
            {
                ushort audioFormat = BitConverter.ToUInt16(wavBytes, pos);
                channels = BitConverter.ToUInt16(wavBytes, pos + 2);
                sampleRate = BitConverter.ToInt32(wavBytes, pos + 4);
                bitsPerSample = BitConverter.ToUInt16(wavBytes, pos + 14);

                if (audioFormat != 1)
                    throw new Exception("PCM WAVのみ対応しています");
            }
            else if (chunkId == "data")
            {
                audioData = new byte[chunkSize];
                Array.Copy(wavBytes, pos, audioData, 0, chunkSize);
                break;
            }

            pos += chunkSize;
        }

        if (audioData == null)
            throw new Exception("WAVのdataチャンクが見つかりません");

        float[] samples = ConvertPcmToFloat(audioData, bitsPerSample);

        int sampleCount = samples.Length / channels;

        AudioClip clip = AudioClip.Create(
            clipName,
            sampleCount,
            channels,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    float[] ConvertPcmToFloat(byte[] data, int bitsPerSample)
    {
        if (bitsPerSample == 16)
        {
            int sampleCount = data.Length / 2;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short value = BitConverter.ToInt16(data, i * 2);
                samples[i] = value / 32768f;
            }

            return samples;
        }

        if (bitsPerSample == 8)
        {
            int sampleCount = data.Length;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (data[i] - 128) / 128f;
            }

            return samples;
        }

        throw new Exception($"未対応のビット深度です: {bitsPerSample}");
    }

    IEnumerator StartSynthesisAudioRoutine(string text)
    {
        yield return RequestAudioQueryRoutine(text);
        yield return SynthesisAudioRoutine();
    }


    /// <summary>
    /// 音声合成を開始する
    /// </summary>
    /// <param name="text">音声合成をするテキスト</param>
    public void StartSynthesisAudio(string text)
    {
        if(this.enabled)
            StartCoroutine(StartSynthesisAudioRoutine(text));
    }
}
