using UnityEngine;
using UnityEngine.UI;

namespace AukiHandTrackerSample
{
    public class HandLandmark : MonoBehaviour
    {
        [SerializeField] private Text infoText;

        public void SetText(string info)
        {
            infoText.text = info;
        }
    }
}