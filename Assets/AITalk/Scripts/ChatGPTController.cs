using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using UnityEngine.Events;

public class ChatGPTController : MonoBehaviour
{
    private string apiKey = "";
    public CheckPanelController.StatusEvent OnCheckAPIKey;
    private string  apiUrl = "https://api.openai.com/v1/chat/completions";

    public ChatGPTMessageModel developerMessage = new ChatGPTMessageModel();
    public CheckPanelController.StatusEvent OnCheckDeveloperFile;

    /// <summary>
    /// 連続した会話を実現するため、ユーザーのメッセージ内容とチャットボットの回答内容を記録しておく
    /// </summary>
    private List<ChatGPTMessageModel> messageList = new List<ChatGPTMessageModel>();
    /// <summary>
    /// メッセージ数が無制限だとトークンが大きくなりすぎるため、記憶しておくメッセージ数の上限を設定する
    /// </summary>
    public int maxMessages = 10;


    private Dictionary<string, string>  headers = new Dictionary<string, string>
    {
        {"Authorization", "Bearer "},
        {"Content-type", "application/json"},
        {"X-Slack-No-Retry", "1"}
    };

    [SerializeField] ChatGPTCompletionRequestModel options = new ChatGPTCompletionRequestModel()
    {
        model = "gpt-5.4"
    };

    /// <summary>
    /// 表情検知正規表現
    /// </summary>
    [SerializeField] string faceRegularExpression = @"表情\[(.*?)\]\n*(.*)";

    /// <summary>
    /// GPTの応答から表情を検知した場合に通知する
    /// </summary>
    public UnityEvent<string> OnExtractFace = new UnityEvent<string>();

    /// <summary>
    /// GPTから受け取ったメッセージを通知する
    /// </summary>
    public UnityEvent<string> OnReceiveMessage = new UnityEvent<string>();

    public void LoadAPIKey()
    {
        try
        {
            this.apiKey = APIKeyManager.Instance.LoadFile("chatgpt");

            // 不要な改行が入っていると、リクエストヘッダーでエラーとなるため削除する
            this.apiKey = this.apiKey.Replace("\n", "").Replace("\r\n", "");
            OnCheckAPIKey.Invoke(
                CheckPanelController.Status.OK
                , "APIKey読み込み完了"
            );
        }catch(Exception e)
        {
            OnCheckAPIKey.Invoke(
                CheckPanelController.Status.Error
                , "APIKeyファイルの読み込みに失敗しました"
            );
            ErrorPopper.PopError(e.Message);
        }
    }

    public void LoadDeveloper()
    {
        try
        {
            var developerText = APIKeyManager.Instance.LoadFile("chatgpt-developer");
            this.developerMessage = new ChatGPTMessageModel()
            {
                role = "developer",
                content = developerText
            };
            OnCheckDeveloperFile.Invoke(
                CheckPanelController.Status.OK
                , "開発者ファイル読み込み完了"
            );
        }catch(Exception e)
        {
            OnCheckDeveloperFile.Invoke(
                CheckPanelController.Status.Error
                , "開発者ファイルの読み込みに失敗しました"
            );
            ErrorPopper.PopError(e.Message);
            
        }
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
        if(this.apiKey == "")
        {
            LoadAPIKey();
        }

        Debug.Log("Start Request ChatGPT:" + message);
        // 送信内容を構築
        AddMessage(message);
        options.messages = new List<ChatGPTMessageModel>() {
            this.developerMessage
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
            // エラーメッセージをユーザーに表示して中止
            ErrorPopper.PopError(request.downloadHandler.text);
            Debug.LogError(request.error);
            return;
        }

        var responseString = request.downloadHandler.text;
        var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString);
        var responseMessage = responseObject.choices[0].message.content;
        Debug.Log("ChatGPT:" + responseMessage);
        AddMessage(responseMessage, true);

        // 表情をメッセージから分離
        string remainingMessage = ExtractFace(responseMessage);
        this.OnReceiveMessage.Invoke(remainingMessage);
    }

    /// <summary>
    /// GPTの返答から表情を抜き出す
    /// </summary>
    /// <param name="message">GPT応答メッセージ</param>
    /// <returns>表情抽出後の残りのメッセージ</returns>
    private string ExtractFace(string message)
    {
        // オプションにSinglelineを設定することで、任意文字が改行にも一致させる
        Regex regex = new Regex(this.faceRegularExpression, RegexOptions.Singleline);
        var match = regex.Match(message);

        //表情を検知できなければそのまま返す
        if (!match.Success)
        {
            return message;
        }

        // 表情を通知
        string face = match.Groups[1].Value;
        Debug.Log($"表情を検知: {face}");
        this.OnExtractFace.Invoke(face);
        
        // 表情以外のメッセージを返す
        return match.Groups[2].Value;
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