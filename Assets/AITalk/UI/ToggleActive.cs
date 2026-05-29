using System;
using UnityEngine;

public class ToggleActive : MonoBehaviour
{
    public void Toggle()
    {
        this.gameObject.SetActive(!this.gameObject.activeSelf);
    }
}
