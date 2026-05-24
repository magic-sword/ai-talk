using TMPro;
using UnityEngine;

public class APIFilePaths : MonoBehaviour
{
    public TextMeshProUGUI APIKeyText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateText()
    {
        var paths = APIKeyManager.Instance.keyPaths;
        APIKeyText.text = JsonUtility.ToJson(paths);
    }

}
