using UnityEngine;
using TMPro;
/// <summary>
/// エラーメッセージを、ユーザーにもわかりやすい形でポップアップする
/// </summary>
public class ErrorPopper : MonoBehaviour
{
    private static ErrorPopper instance;

    TMP_Text text;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            gameObject.SetActive(false);    //初期は非表示
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ShowMessage(string message)
    {
        text.text = message;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ポップアップ表示でエラーメッセージを表示する
    /// </summary>
    public static void PopError(string message)
    {
        ErrorPopper.instance.ShowMessage(message);
    }

    /// <summary>
    /// エラーメッセージをクリップボードへコピーする
    /// </summary>
    public void CopyToClip()
    {
        GUIUtility.systemCopyBuffer = text.text;
    }
}
