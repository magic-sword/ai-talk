using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class YouTubeLiveController : MonoBehaviour
{
    public int port = 8081;

    /// <summary>
    /// 認証完了時のリダイレクトを受け取るリスナー
    /// </summary>
    private HttpListener listener = new HttpListener();

    /// <summary>
    /// 認証完了時にWebブラウザへ表示するHTMLファイル
    /// </summary>
    public TextAsset completedHTML;

    /// <summary>
    /// Youtubeへの認証情報に必要なデータ
    /// 機密情報なため、外部ファイルとして保管しGit上にはコミットしない
    /// </summary>
    private APIKey apiKey;

    private string authCode = ""; // API Keyから取得した認証コード

    public string liveId = "xxx";

    private string chatId = ""; // 配信中のチャットID

    private Token token = null; // 認証コードから取得されるトークン

    private LiveBroadcast targetLive = null;    // 連携対象ライブ
    /// <summary>
    /// Youtube配信からコメントを受け取った場合に通知する
    /// </summary>
    public  UnityEvent<string> onCommentEvent;

    /// <summary>
    /// Youtube配信一覧を取得した場合に通知する
    /// </summary>
    public  UnityEvent<List<LiveBroadcast>> onLiveList;

    private string RedirectUri
    {
        get
        {
            // リダイレクトを受け取るポート番号が競合しないように指定しておく
            return $"{this.apiKey.installed.redirect_uris[0]}:{port}/";
        }
    }

    /// <summary>
    /// 認証情報でヘッダーに記載する必要がある文字列
    /// </summary>
    private string Authorization
    {
        get
        {
            return $"Bearer {token.access_token}";
        }
    }

    public void SetTarget(LiveBroadcast target)
    {
        this.targetLive = target;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }
    
    private IEnumerator WaitRedirectRoutine()
    {
        listener.Prefixes.Add(this.RedirectUri);
        listener.Start();
        try
        {
            // リダイレクト待機
            Task<HttpListenerContext> task = listener.GetContextAsync();
            yield return new WaitUntil(() => task.IsCompleted);
            HttpListenerContext context = task.Result;
            
            // 結果受け取り
            var queryParams = HttpUtility.ParseQueryString(context.Request.Url.Query);
            this.authCode = queryParams["code"];

            // ブラウザへ応答
            byte[] buffer = completedHTML.bytes;
            context.Response.ContentLength64 = buffer.Length;
            yield return context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        finally
        {
            listener.Stop();
        }
    }
    public void StartAuth()
    {
        var text = APIKeyManager.Instance.LoadFile("youtube");
        this.apiKey = JsonUtility.FromJson<APIKey>(text);

        var web = this.apiKey.installed;
        var queryString = HttpUtility.ParseQueryString("");
        queryString.Add("response_type", "code");  // 認証コードの返却パラメータを指定
        queryString.Add("client_id", web.client_id);  // アプリケーションのクライアント ID
        queryString.Add("redirect_uri", this.RedirectUri);  // 認可フロー完了後にリダイレクトする場所を指定
        queryString.Add("scope", "https://www.googleapis.com/auth/youtube.readonly");  // アクセスするリソースを指定
        queryString.Add("access_type", "offline"); // アクセストークンの更新が必要になったとき、ユーザーがブラウザにいなくても更新可能にする

        // URIとクエリをマージ
        var uriBuilder = new UriBuilder(web.auth_uri) {
            Query = queryString.ToString()
        };
        var auth_uri = uriBuilder.Uri.ToString();

        Debug.Log($"authUrl: {auth_uri}");
        Application.OpenURL(auth_uri);

        // 認証完了後のリダイレクトを待機
        StartCoroutine(WaitRedirectRoutine());
    }


    private IEnumerator SendWebRequest(UnityWebRequest request)
    {
        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.Success)
        {
            yield break;
        }

        // エラー内容を出力
        Debug.LogError(request.downloadHandler.text);
    }

    public void RequestLiveList()
    {
        StartCoroutine(RequestLiveRoutine());
    }

    private IEnumerator RequestLiveRoutine()
    {
        yield return RequestToken();
        yield return RequesLiveList();
    }
    private IEnumerator RequestToken()
    {
        var web = this.apiKey.installed;
        var tokenUrl = "https://oauth2.googleapis.com/token";
        var content = new Dictionary<string,string> () {
            { "code", this.authCode },
            { "client_id", web.client_id },
            { "client_secret", web.client_secret },
            { "redirect_uri",  this.RedirectUri },
            { "grant_type", "authorization_code" },
            { "access_type", "offline" },
        };
        var req = UnityWebRequest.Post (tokenUrl, content);
        yield return SendWebRequest(req);

        this.token = JsonUtility.FromJson<Token>(req.downloadHandler.text); 
    }

    /// <summary>
    /// 自分が配信中のライブ一覧を取得する
    /// アクセストークンの事前取得が必要
    /// </summary>
    /// <returns></returns>
    private IEnumerator RequesLiveList()
    {
        // GETパラメータを構築
        var queryString = HttpUtility.ParseQueryString("");
        queryString.Add("part", "snippet,status");  // レスポンスに含めるリソースプロパティ(カンマ区切り)
        queryString.Add("mine", "true");  // フィルタ:認証されたユーザーが所有する

        // URIとクエリをマージ
		var uriBuilder = new System.UriBuilder("https://www.googleapis.com/youtube/v3/liveBroadcasts") {
			Query = queryString.ToString()
		};

        using var req = UnityWebRequest.Get(uriBuilder.Uri);
        
        req.SetRequestHeader ("Authorization", this.Authorization);
        yield return SendWebRequest(req);

        var resource = JsonConvert.DeserializeObject<BroadcastResource>(req.downloadHandler.text);
        this.onLiveList.Invoke(new List<LiveBroadcast>(resource.items));
    }

    /// <summary>
    /// YouTube Live Streaming APIを使って、ライブ中のチャットのIDを取得する
    /// 配信中のライブIDの指定が必要
    /// 認証された接続トークンを取得しておく必要がある
    /// </summary>
    private IEnumerator RequestChatId()
    {
        // GETパラメータを構築
        var queryString = HttpUtility.ParseQueryString("");
        queryString.Add("part", "snippet");  // レスポンスに含めるリソースプロパティ(カンマ区切り)
        queryString.Add("id", this.liveId);  // ライブID

        // URIとクエリをマージ
		var uriBuilder = new System.UriBuilder("https://www.googleapis.com/youtube/v3/liveBroadcasts") {
			Query = queryString.ToString()
		};

        var req = UnityWebRequest.Get(uriBuilder.Uri);
        req.SetRequestHeader ("Authorization", this.Authorization);
        yield return SendWebRequest(req);
        var liveBroadcast = JsonUtility.FromJson<BroadcastResource> (req.downloadHandler.text);

        this.chatId = liveBroadcast.items[0].snippet.liveChatId;
    }

    public void StartCommentRoutine()
    {
        StartCoroutine(RequestCommentRoutine());
    }
    private IEnumerator RequestCommentRoutine()
    {
        Debug.Log("Start RequestCommentRoutine");

        string page_token = "";
        while (this.enabled)
        {
            // GETパラメータを構築
            var queryString = HttpUtility.ParseQueryString("");
            queryString.Add("part", "snippet,authorDetails"); // レスポンスに含めるリソースプロパティ(カンマ区切り)
            queryString.Add("liveChatId", this.targetLive.snippet.liveChatId);  // コメントを取得する対象のチャット
            queryString.Add("page_token", page_token);  // ページトークンを指定することで、前回からの差分コメントのみ取得できる

            // URIとクエリをマージ
            var uriBuilder = new UriBuilder("https://www.googleapis.com/youtube/v3/liveChat/messages") {
                Query = queryString.ToString()
            };
            
            var req = UnityWebRequest.Get(uriBuilder.Uri);
            req.SetRequestHeader ("Authorization", this.Authorization);
            yield return SendWebRequest(req);

            var liveChat = JsonUtility.FromJson<BroadcastResource> (req.downloadHandler.text);
            var comment = FormatComment(liveChat.items);
            if(comment != "")
            {
                Debug.Log($"Receve Comments:\n {comment}");
                this.onCommentEvent.Invoke(comment);    // コメントの取得を通知
            }

            page_token = liveChat.nextPageToken;
            yield return new WaitForSeconds(1); // ループ処理を1秒待機
        }
    }

    /// <summary>
    /// 受け取ったコメントを1つの文字列にまとめる
    /// コメント:(リスナー表示名)「コメント内容」
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    private string FormatComment(LiveBroadcast[] items)
    {
        var comment = "";

        foreach (var item in items) {
            var snip = item.snippet;
            var author = item.authorDetails;

            comment += $"コメント:({author.displayName})「{snip.displayMessage}」\n";
        }
        return comment;
    }


    // Update is called once per frame
    void Update()
    {
        
    }


    [Serializable]
    public class Token
    {
        public string access_token;
    }

    [Serializable]
    public class BroadcastResource
    {
        public LiveBroadcast[] items;
        public string nextPageToken;
    }

    [Serializable]
    public class LiveBroadcast
    {
        public Snippet snippet;
        public Author authorDetails;
    }

    [Serializable]
    public class Snippet
    {
        public string title;
        public string liveChatId;
        public string displayMessage;
        public Dictionary<string, Thumbnail> thumbnails; // key: サムネイル名
    }

    [Serializable]
    public class Author
    {
        public string displayName;
    }

    [Serializable]
    public class Thumbnail
    {
        public string url;
        public int width;
        public int  height;
    }
}
