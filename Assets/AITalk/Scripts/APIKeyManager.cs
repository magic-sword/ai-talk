using System.Collections.Generic;
using UnityEngine;
using SFB;
using UnityEngine.Networking;

public class APIKeyManager : MonoBehaviour
{
    [System.Serializable]
    public class KeyValue
    {
        public string key;
        public string value;
    }

    [System.Serializable]
    public class SerializableDictionary
    {
        public List<KeyValue> keyValues = new List<KeyValue>();
            // インデクサーの定義
        public string this[string key]
        {
            get
            { 
                var finded = keyValues.Find(entry => entry.key == key);
                if(finded == null)
                {
                    Debug.LogWarning($"key:{key} が辞書内に存在しません");
                    return null;
                }
                return finded.value;
            }
            set 
            { 
                var finded = keyValues.Find(entry => entry.key == key);
                if(finded == null)
                {
                    var entry = new KeyValue();
                    entry.key = key;
                    entry.value = value;
                    keyValues.Add(entry);
                    return;
                }
                finded.value = value; 
            } 
        }
    }


    public static APIKeyManager Instance;
    
    public SerializableDictionary keyPaths = new SerializableDictionary();

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

    public void AddFileByPanel(string key)
    {
        var extensions = new[]
        {
            new ExtensionFilter( "API Key Files", "txt", "json"),
        };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select API Key File", "", extensions, false);
        this.keyPaths[key] = paths[0];
    }

    public string LoadFile(string key)
    {
        string filePath = this.keyPaths[key];
        using (var uwr = UnityWebRequest.Get(filePath))
        {
            uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                 Debug.LogError(uwr.error);
                 return "";
            }

            string text = uwr.downloadHandler.text;
            return text;
        }
    }
}
