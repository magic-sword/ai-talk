using System;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    [SerializeField] List<Face> faces = new List<Face>();
    
    /// <summary>
    /// 表情として管理対象外とする子オブジェクト
    /// </summary>
    [SerializeField] List<GameObject> ignoreList = new List<GameObject>();

    void Start()
    {
        // 子オブジェクトを初期設定として読み込む
        foreach(Transform child in transform)
        {
            if (ignoreList.Contains(child.gameObject))
            {
                // 無視して管理外とする
                return;
            }
            faces.Add(new Face()
            {
                key = child.name
                , obj = child.gameObject
            });
        }
    }

    /// <summary>
    /// 設定された表情のみ有効化する
    /// </summary>
    /// <param name="name"></param>
    public void SetFace(string name)
    {
        foreach (var face in faces)
        {
            if(face.key == name){
                face.obj.SetActive(true);
            }
            else
            {
                face.obj.SetActive(false);
            }
        }
    }

    [Serializable]
    public class Face
    {
        public string key;
        public GameObject obj;
    }
}
