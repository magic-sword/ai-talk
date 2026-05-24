using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class OnListenEvent : UnityEvent<HttpListenerContext> { }

public class HttpServer : MonoBehaviour
{
    private HttpListener httpListener = new HttpListener();
    private Thread listenerThread;

    public int port = 8081;
    public string path = "/";

    /// <summary>
    /// リクエストを受信した際に発火するイベントハンドラー。エディター上から設定する。
    /// </summary>
    public OnListenEvent OnListen;
    
    void Start()
    {
        var prefixes = "http://*:" + port + path;
        httpListener.Prefixes.Add(prefixes);
        httpListener.Start();

        // 非同期処理用のスレッドを開始
        listenerThread = new Thread(StartListening);
        listenerThread.Start();
    }

    private void StartListening()
    {
        try
        {
            while (httpListener.IsListening)
            {
                Debug.Log("Listening Http");
                // リクエストを非同期で待機し、完了したらコールバックを呼び出す
                IAsyncResult result = httpListener.BeginGetContext(ListenerCallback, httpListener);
                // 次のリクエストを処理するためにメインスレッドの処理をブロックしないよう待機
                result.AsyncWaitHandle.WaitOne();
            }
        }
        catch (Exception e) {
            Debug.LogError(e);
        }
    }

    private void ListenerCallback(IAsyncResult result)
    {
        HttpListener listener = (HttpListener)result.AsyncState;
        HttpListenerContext context = listener.EndGetContext(result);

        Debug.Log("OnListen Invoke");
        OnListen.Invoke(context);
    }
    // 破棄時にサーバーを止める
    void OnDestroy()
    {
        // 破棄時にスレッドとリスナーを確実に終了
        if (listenerThread != null)
        {
            listenerThread.Abort();
        }
        if (httpListener != null)
        {
            httpListener.Stop();
            httpListener.Close();
        }
    }
}
