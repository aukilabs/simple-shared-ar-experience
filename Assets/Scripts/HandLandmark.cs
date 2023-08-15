using UnityEngine;
using UnityEngine.UI;


public class HandLandmark : MonoBehaviour
{
    [SerializeField] private Text infoText;

    public void SetInfo(string info)
    {
        infoText.text = info;
    }
}
