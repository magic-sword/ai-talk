using UnityEngine;
using UnityEngine.Events;

public class ToggleSetting : MonoBehaviour
{
    [SerializeField] bool isActive = true;

    public UnityEvent<bool> OnToggle;

    public void Toggle()
    {
        this.isActive = ! this.isActive;

        OnToggle.Invoke(this.isActive);
    }

    void Start()
    {
        OnToggle.Invoke(this.isActive);
    }
}
