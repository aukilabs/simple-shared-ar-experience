using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AukiHandTrackerSample
{
    public class TouchableByHand : MonoBehaviour
    {
        public Main mainScript;
        private GameObject raccoonObject;
        private Animator raccoonAnimator;

        private void Awake()
        {
            mainScript = FindObjectOfType<Main>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (mainScript == null)
            {
                mainScript = FindObjectOfType<Main>();
                if (mainScript == null)
                {
                    Debug.LogError("Main script not found");
                    return;
                }
            }

            if (mainScript.hasPlayedDead)
            {
                mainScript.GetUp();
            }
        }
    }
}