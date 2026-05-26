using System.Collections.Generic;
using UnityEngine;

public class BroadcastListView : MonoBehaviour
{
    [SerializeField]
    private BroadcastView prefab;

    private List<BroadcastView> views = new();

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
            var view = Instantiate(prefab, this.transform);
            view.Broadcast = data;
            views.Add(view);
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
