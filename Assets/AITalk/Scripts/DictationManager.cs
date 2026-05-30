using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;

public class DictationManager : MonoBehaviour
{
    private DictationRecognizer dictationRecognizer;

    bool isRunnning;

    /// <summary>
    /// 送信する際にGPTへ伝えるプレイヤー名前
    /// </summary>
    [SerializeField] string playerName = "";

    /// <summary>
    /// フレーズが認識されたときに通知する
    /// </summary>
    public UnityEvent<string> OnDictate;

    /// <summary>
    /// 音声認識を受け付ける精度レベル
    /// High = 0
    /// Medium = 1
    /// Low = 2
    /// Rejected = 3
    /// </summary>
    public ConfidenceLevel level = ConfidenceLevel.Medium;

    /// <summary>
    /// マイクへのアクセス許可がない場合に発生するエラーに対するメッセージ
    /// </summary>
    [SerializeField]
    private TextAsset errMicAccessMessage;

    /// <summary>
    /// 音声認識を開始する
    /// </summary>
    public void StartDictate()
    {
        try
        {
            isRunnning = true;
            dictationRecognizer.Start();
        }
        catch (Exception e)
        {
            ErrorPopper.PopError($"{errMicAccessMessage} \n\n {e.Message}");
        }
    }

    /// <summary>
    /// 音声認識を停止する
    /// </summary>
    public void StopDictate()
    {
        try
        {
            isRunnning = false;
            dictationRecognizer.Stop();
        }
        catch (Exception e)
        {
            ErrorPopper.PopError($"{errMicAccessMessage} \n\n {e.Message}");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += this.FilterLevel;
        dictationRecognizer.DictationComplete += this.OnDictationComplete;
        dictationRecognizer.DictationError += this.OnDictationError;
    }

    public void SetPlayerName(string name)
    {
        this.playerName = name;
    }

    /// <summary>
    /// メッセージ送信を実行する
    /// </summary>
    /// <param name="message"></param>
    public void InvokeMessage(string message)
    {
        
        this.OnDictate.Invoke($"{playerName}「{message}」");
    }

    /// <summary>
    /// 音声認識の精度をフィルタして通知する
    /// </summary>
    /// <param name="text"></param>
    /// <param name="confidence"></param>
    void FilterLevel(string text, ConfidenceLevel confidence)
    {
        Debug.Log($"DictationResult: {confidence}\n{text}");
        // 設定よりも認識精度が高ければ通知する
        if(confidence <= this.level)
        {
            InvokeMessage(text);
        }
    }

    void OnDictationComplete(DictationCompletionCause cause)
    {
        // 音声認識がタイムアウトしたら再開することで継続する
        // 実行設定が無効なら再開しない
        if(isRunnning)
            dictationRecognizer.Start();
    }

    /// <summary>
    /// 音声認識でエラーが発生した際の処理
    /// </summary>
    /// <param name="error"></param>
    /// <param name="hresult"></param>
    void OnDictationError(string error, int hresult)
    {
        ErrorPopper.PopError($"{errMicAccessMessage} \n\n {error}");
        Debug.LogError(error);
    }

    private void OnDestroy()
    {
        // オブジェクト破棄時に音声認識も停止
        dictationRecognizer.Dispose();
    }

}
