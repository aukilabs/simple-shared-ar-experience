using System;
using UnityEngine;

public class TouchableByHand : MonoBehaviour
{
    public event Action OnTouched;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("hand"))
        {
            OnTouched?.Invoke();
        }
    }
}