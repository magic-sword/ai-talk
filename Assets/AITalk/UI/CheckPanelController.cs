using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CheckPanelController : MonoBehaviour
{
    [SerializeField]
    Image okImage;

    [SerializeField]
    Image errorImage;

    [SerializeField]
    TextMeshProUGUI text;
    private void SetOK()
    {
        okImage.gameObject.SetActive(true);
        errorImage.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
    }

    private void SetError()
    {
        okImage.gameObject.SetActive(false);
        errorImage.gameObject.SetActive(true);
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// 待機状態を表す
    /// </summary>
    private void SetIdle()
    {
        this.gameObject.SetActive(false);
    }
    private void SetMessage(string messages)
    {
        text.text = messages;
    }

    public void SetStatus(Status status, string messages)
    {
        switch(status) {
            case Status.Idle:
                SetIdle();
                break;
            case Status.OK:
                SetOK();
                break;
            case Status.Error:
                SetError();
                break;
        }
        SetMessage(messages);
    }

    public void SetStatus(bool ok)
    {
        if (ok)
        {
            SetOK();
        }
        else
        {
            SetError();
        }
    }

    public enum Status
    {
        Idle
        , OK
        , Error
    }

    [Serializable]
    public class StatusEvent :  UnityEvent<Status, string>{};
}
