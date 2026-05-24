using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;

public class YouTubeLiveController : MonoBehaviour
{
    public int port = 8081;

    /// <summary>
    /// Youtubeへの認証情報に必要なデータ
    /// 機密情報なため、外部ファイルとして保管しGit上にはコミットしない
    /// </summary>
    private APIKey apiKey;

    public string liveId = "xxx";

    private SynchronizationContext mainContext;

    [Serializable]
    public class Token
    {
        public string access_token;
    }

    [Serializable]
    public class LiveBroadcast
    {
        public Item[] items;

        public string nextPageToken;

        [Serializable]
        public class Item
        {
            public Snippet snippet;

            public Author authorDetails;

            [Serializable]
            public class Snippet
            {
                public string liveChatId;
                public string displayMessage;
            }

            [Serializable]
            public class Author
            {
                public string displayName;
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // メインスレッドで実行しておく
        mainContext = SynchronizationContext.Current;
    }

    public void StartAuth()
    {
        var text = APIKeyManager.Instance.LoadFile("youtube");

        this.apiKey = JsonUtility.FromJson<APIKey>(text);

        var web = this.apiKey.web;
        var queryString = System.Web.HttpUtility.ParseQueryString("");
        queryString.Add("response_type", "code");  // 認証コードの返却パラメータを指定
        queryString.Add("client_id", web.client_id);  // アプリケーションのクライアント ID
        queryString.Add("redirect_uri", web.redirect_uris[0]);  // 認可フロー完了後にリダイレクトする場所を指定
        queryString.Add("scope", "https://www.googleapis.com/auth/youtube.readonly");  // アクセスするリソースを指定
        queryString.Add("access_type", "offline"); // アクセストークンの更新が必要になったとき、ユーザーがブラウザにいなくても更新可能にする

        // URIとクエリをマージ
        var uriBuilder = new System.UriBuilder(web.auth_uri) {
			Query = queryString.ToString()
		};
        var auth_uri = uriBuilder.Uri.ToString();

        Debug.Log($"authUrl: {auth_uri}");
        Application.OpenURL(auth_uri);
    }

    public void onReceiveAuthCode(HttpListenerContext context)
    {
        Debug.Log($"onReceiveAuthCode:{context}");
        var req = context.Request;

        var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
        var code = queryParams["code"];

        // asynchronously メインスレッドで実行
        mainContext.Post((_) =>
        {
            try
            {
                StartCoroutine(GetTokenCoroutine(queryParams["code"]));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }, null);

    }
    IEnumerator GetTokenCoroutine(string authCode)
    {
        var web = this.apiKey.web;
        var tokenUrl = "https://oauth2.googleapis.com/token";
        var content = new Dictionary<string,string> () {
            { "code", authCode },
            { "client_id", web.client_id },
            { "client_secret", web.client_secret },
            { "redirect_uri",  web.redirect_uris[0] },
            { "grant_type", "authorization_code" },
            { "access_type", "offline" },
        };
        var request = UnityWebRequest.Post (tokenUrl, content);
        yield return request.SendWebRequest();

        var token = JsonUtility.FromJson<Token>(request.downloadHandler.text).access_token;

        Debug.Log (token);
        // Youtube配信APIへアクセスするため、Live Streaming API/List のリクエストを設定
        // GETパラメータを構築
        var queryString = System.Web.HttpUtility.ParseQueryString("");
        queryString.Add("part", "snippet");  // レスポンスに含めるリソースプロパティ(カンマ区切り)
        queryString.Add("id", this.liveId);  // YouTube配信ID

        // URIとクエリをマージ
		var uriBuilder = new System.UriBuilder("https://www.googleapis.com/youtube/v3/liveBroadcasts") {
			Query = queryString.ToString()
		};

        var req = UnityWebRequest.Get (uriBuilder.Uri);
        req.SetRequestHeader ("Authorization", "Bearer " + token);
        yield return  req.SendWebRequest();

        var liveBroadcast = JsonUtility.FromJson<LiveBroadcast> (req.downloadHandler.text);

        var chatId = liveBroadcast.items[0].snippet.liveChatId;

        Debug.Log (chatId);

        var url = "https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet,authorDetails";
        url += "&liveChatId=" + chatId;

        req = UnityWebRequest.Get (url);
        req.SetRequestHeader ("Authorization", "Bearer " + token);
        yield return req.SendWebRequest();

        var liveChat = JsonUtility.FromJson<LiveBroadcast> (req.downloadHandler.text);
        var items = liveChat.items;

        foreach (var item in items) {
        var snip = item.snippet;
        var author = item.authorDetails;
        Debug.Log (author.displayName + ": "
            + snip.displayMessage);
        }
        Debug.Log (liveChat.nextPageToken);
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
