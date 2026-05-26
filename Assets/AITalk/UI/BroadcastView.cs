using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BroadcastView : MonoBehaviour
{
    [SerializeField]
    private YouTubeLiveController.LiveBroadcast broadcast;

    public  RawImage thumbnail;
    public  TextMeshProUGUI title;

    public UnityEvent<YouTubeLiveController.LiveBroadcast> OnClick;

    public YouTubeLiveController.LiveBroadcast Broadcast
    {
        get
        {
            return broadcast;
        }
        set
        {
            StartCoroutine(SetBroadcast(value));
        }
    }

    public IEnumerator SetBroadcast(YouTubeLiveController.LiveBroadcast broadcast)
    {
        this.broadcast = broadcast;

        var thumbnailResource = this.broadcast.snippet.thumbnails.First();
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(thumbnailResource.Value.url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        this.thumbnail.texture = texture;
        title.text = broadcast.snippet.title;
    }

    public void Click()
    {
        // クリック通知
        OnClick.Invoke(this.broadcast);
    }
}
