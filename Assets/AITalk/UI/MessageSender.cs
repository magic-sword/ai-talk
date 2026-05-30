using UnityEngine;
using TMPro;
using UnityEngine.Events;
public class MessageSender : MonoBehaviour
{
    public  TextMeshProUGUI message;

    public UnityEvent<string> target;

    public void Send()
    {
        target.Invoke(message.text);
    }
}
