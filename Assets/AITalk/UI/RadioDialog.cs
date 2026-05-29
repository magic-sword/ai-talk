using System;
using System.Collections.Generic;
using UnityEngine;

public class RadioDialog : MonoBehaviour
{
    [SerializeField] List<KeyValue> choices;

    public void Choice(string key)
    {
        foreach (var keyvalue in choices)
        {
            if(keyvalue.key == key)
            {
                keyvalue.value.SetActive(true);
            }
            else
            {
                keyvalue.value.SetActive(false);
            }
        }
    }

    [Serializable]
    public class KeyValue
    {
        public string key;
        public GameObject value;
    }
}
