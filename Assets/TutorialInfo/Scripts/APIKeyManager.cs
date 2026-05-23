using System.Collections.Generic;
using UnityEngine;
using SFB;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System;

public class APIKeyManager : MonoBehaviour
{
    public static APIKeyManager Instance;
    
    private Dictionary<string, string> keyPaths = new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFileByOpen(string key)
    {
        var extensions = new[]
        {
            new ExtensionFilter( "API Key Files", "json"),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select API Key File", "", extensions, false);
        this.keyPaths.Add(key, paths[0]);
    }

    private IEnumerator LoadRoutine<T>(string uri, UnityAction<T> callback) {
        using (var uwr = UnityWebRequest.Get(uri))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                 Debug.LogError(uwr.error);
            }
            else
            {
                string text = uwr.downloadHandler.text;
                T jsonObject = JsonUtility.FromJson<T>(text);

                // 読み込み完了のコールバックを実行
                callback(jsonObject);
            }
        }
    }

    /// <summary>
    /// 登録されているJSONファイルの中から、目的のファイルを探索する
    /// </summary>
    /// <typeparam name="T">パーズするJSON クラス</typeparam>
    /// <param name="key">jsonファイルの名前</param>
    /// <param name="callback">見つかった場合に呼び出す関数</param>
    public void FindAPIKey<T>(string key, UnityAction<T> callback)
    {
        string filePath = "";
        if (keyPaths.TryGetValue(key, out filePath))
        {
            StartCoroutine(LoadRoutine<T>(filePath, callback));
        }
        else
        {
            Debug.LogWarning($"指定したkey({key})が存在しません");
        }
    }

    public void OnClick()
    {
        AddFileByOpen("youtube");
    }
}
