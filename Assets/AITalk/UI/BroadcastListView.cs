using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BroadcastListView : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;

    private List<BroadcastView> views = new();

    /// <summary>
    /// ライブ配信が選択された際に通知する
    /// </summary>
    public UnityEvent<YouTubeLiveController.LiveBroadcast> OnSelect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetList(List<YouTubeLiveController.LiveBroadcast> list)
    {
        Clear();

        foreach(var data in list)
        {
            var view = Instantiate(prefab, this.transform); // プレハブからUIをクローン
            var target = view.GetComponent<BroadcastView>();

            target.Broadcast = data;    // UIへ表示データを設定
            target.OnClick.AddListener(OnSelect.Invoke);    // クリック通知と選択通知を繋げる

            views.Add(target);
        }
    }

    private void Clear()
    {
        foreach(var view in views)
        {
            Destroy(view.gameObject);
        }

        views.Clear();
    }
}
