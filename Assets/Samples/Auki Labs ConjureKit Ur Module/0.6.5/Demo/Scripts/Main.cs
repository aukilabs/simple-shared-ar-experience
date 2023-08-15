using System.Collections.Generic;
using Auki.Ur;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace AukiHandTrackerSample
{
    /// <summary>
    /// This sample shows how to use Ur hand tracker for the most common cases:
    /// - Showing hand calibration state.
    /// - Calibrating hand tracker for specific hand size.
    /// - Visualizing the hand landmarks
    /// </summary>
    public class Main : MonoBehaviour
    {
        private const int NumberOfTrackedHands = 1;
        
        /// References needed to initialize the hand tracker
        [SerializeField] private Camera arCamera;
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARRaycastManager arRaycastManager;
        
        [SerializeField] private HandLandmark handLandmarkPrefab;
        
        private HandTracker _handTracker;
        private List<HandLandmark> _handLandmarks;

        private void Start()
        {
            _handTracker = HandTracker.GetInstance();

            // Initialize the hand tracker
            _handTracker.SetARSystem(arSession, arCamera, arRaycastManager);

            // Initialize a list of HandLandmarks to display landmark index and position
            _handLandmarks = new List<HandLandmark>(NumberOfTrackedHands * HandTracker.LandmarksCount);
            for (int i = 0; i < HandTracker.LandmarksCount; i++)
            {
                _handLandmarks.Add(Instantiate(handLandmarkPrefab));
                _handLandmarks[i].SetText(i.ToString());
                _handLandmarks[i].transform.SetParent(arCamera.transform);
            }

            _handTracker.OnUpdate += (landmarks, translations, isRightHand, score) =>
            {
                for (int h = 0; h < NumberOfTrackedHands; ++h)
                {
                    // Toggle the visibility of the hand landmarks based on the confidence score of the hand detection
                    ToggleHandLandmarks(score[h] > 0);
                    if (score[h] > 0)
                    {
                        var handPosition = new Vector3(
                            translations[h * 3 + 0], 
                            translations[h * 3 + 1],
                            translations[h * 3 + 2]);

                        var handLandmarkIndex = h * HandTracker.LandmarksCount * 3;
                        for (int l = 0; l < HandTracker.LandmarksCount; ++l)
                        {
                            var landMarkPosition = new Vector3(
                                landmarks[handLandmarkIndex + (l * 3) + 0],
                                landmarks[handLandmarkIndex + (l * 3) + 1],
                                landmarks[handLandmarkIndex + (l * 3) + 2]);
                    
                            // Update the landmarks position 
                            _handLandmarks[l].transform.localPosition = handPosition + landMarkPosition;
                        }
                    }
                }
            };

            _handTracker.Start(NumberOfTrackedHands);
        }

        private void Update()
        {
            _handTracker.Update();
        }

        private void ToggleHandLandmarks(bool visible)
        {
            foreach (var handLandmark in _handLandmarks)
            {
                handLandmark.gameObject.SetActive(visible);
            }
        }
    }
}