using UnityEngine;
using UnityEngine.UI;

public class ToggleColor : MonoBehaviour
{
    [SerializeField] Color startColor = Color.green;
    [SerializeField] Color stopColor = Color.red;
    [SerializeField] Image target;

    public void SetStatu(bool status)
    {
        if (status)
        {
            target.color = startColor;
        }
        else
        {
            target.color = stopColor;
        }
    }
}
