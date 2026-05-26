using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class ChatGPTController : MonoBehaviour
{
    private string apiKey;
    private string  apiUrl = "https://api.openai.com/v1/chat/completions";

    public ChatGPTMessageModel systemMessage = new ChatGPTMessageModel();

    /// <summary>
    /// 連続した会話を実現するため、ユーザーのメッセージ内容とチャットボットの回答内容を記録しておく
    /// </summary>
    private List<ChatGPTMessageModel> messageList = new List<ChatGPTMessageModel>();
    /// <summary>
    /// メッセージ数が無制限だとトークンが大きくなりすぎるため、記憶しておくメッセージ数の上限を設定する
    /// </summary>
    public int maxMessages = 10;

    public string testMessage = "";

    private Dictionary<string, string>  headers = new Dictionary<string, string>
    {
        {"Authorization", "Bearer "},
        {"Content-type", "application/json"},
        {"X-Slack-No-Retry", "1"}
    };

    public ChatGPTCompletionRequestModel options = new ChatGPTCompletionRequestModel()
    {
        model = "gpt-5.4"
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadAPIKey()
    {
        this.apiKey = APIKeyManager.Instance.LoadFile("chatgpt");

        // 不要な改行が入っていると、リクエストヘッダーでエラーとなるため削除する
        this.apiKey = this.apiKey.Replace("\n", "").Replace("\r\n", "");
    }

    public void LoadSystem()
    {
        var systemText = APIKeyManager.Instance.LoadFile("chatgpt-system");
        this.systemMessage = new ChatGPTMessageModel()
        {
            role = "system",
            content = systemText
        };
    }

    /// <summary>
    /// メッセージリストにメッセージを追加する
    /// 上限を超えた場合は古いものから削除する
    /// </summary>
    /// <param name="message">メッセージ内容</param>
    /// <param name="isAssistantrole">tureの場合、roleをassistant(チャットボット回答)とする</param>
    private void AddMessage(string message, bool isAssistantrole = false)
    {
        var model = new ChatGPTMessageModel();

        if (isAssistantrole){
            model.role = "assistant";
        }
        else{
            model.role = "user";
        }

        model.content = message;
        this.messageList.Add(model);

        // 上限を超えたら古いメッセージを削除する
        if(this.messageList.Count > this.maxMessages)
        {
            this.messageList.RemoveAt(0);
        }
    }

    public async void RequestAsync(string message)
    {
        Debug.Log("Start Request ChatGPT:" + message);
        // 送信内容を構築
        AddMessage(message);
        options.messages = new List<ChatGPTMessageModel>() {
            this.systemMessage
        };
        options.messages.AddRange(this.messageList);
        var jsonOptions = JsonUtility.ToJson(this.options);

        using var request = new UnityWebRequest(apiUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonOptions)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        // ヘッダー設定
        foreach (var header in this.headers)
        {
            if (header.Key == "Authorization")
            {
                // 認証情報にAPIキーを追加する
                request.SetRequestHeader(header.Key, header.Value + this.apiKey);
            }
            else
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
        }

        // 時間がかかる処理を待機
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
            throw new Exception(request.error);
        }

        var responseString = request.downloadHandler.text;
        var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString);
        var responseMessage = responseObject.choices[0].message.content;
        Debug.Log("ChatGPT:" + responseMessage);
        AddMessage(responseMessage, true);
    }

    public async void SendTestMessage(){
        RequestAsync(this.testMessage);
    }

    [Serializable]
    public class ChatGPTMessageModel
    {
        public string role;
        public string content;
    }

    //ChatGPT APIにRequestを送るためのJSON用クラス
    [Serializable]
    public class ChatGPTCompletionRequestModel
    {
        public string model;
        public List<ChatGPTMessageModel> messages;
    }

    //ChatGPT APIからのResponseを受け取るためのクラス
    [System.Serializable]
    public class ChatGPTResponseModel
    {
        public string id;
        public string @object;
        public int created;
        public Choice[] choices;
        public Usage usage;

        [System.Serializable]
        public class Choice
        {
            public int index;
            public ChatGPTMessageModel message;
            public string finish_reason;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }
}