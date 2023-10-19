using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using Auki.Ur;
using Auki.Util;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
        
        [SerializeField] private Text sessionState;
        [SerializeField] private Text sessionID;
    
        [SerializeField] private GameObject raccoon;
        [SerializeField] private Button spawnButton;
    
        private bool qrCodeBool;
        [SerializeField] Button qrCodeButton;
    
        private IConjureKit _conjureKit;
        private Manna _manna;
    
        private ARCameraManager arCameraManager;
        private Texture2D _videoTexture;
        
        [SerializeField] private HandLandmark handLandmarkPrefab;
        
        private HandTracker _handTracker;
        private List<HandLandmark> _handLandmarks;

        private Vector3[] handLandmarksPositions;
        [SerializeField] private Renderer fingertipLandmark;

        public bool hasPlayedDead = false;
        private GameObject raccoonObject;
        private Animator raccoonAnimator;

        private void Start()
        {
            arCameraManager = arCamera.GetComponent<ARCameraManager>();
        
            _conjureKit = new ConjureKit(
                arCamera.transform,
                "YOUR_APP_KEY",
                "YOUR_APP_SECRET");
    
            _manna = new Manna(_conjureKit);
        
            _conjureKit.OnStateChanged += state =>
            {
                sessionState.text = state.ToString();
                ToggleControlsState(state == State.Calibrated);
            };
    
            _conjureKit.OnJoined += session =>
            {
                sessionID.text = session.Id.ToString();
            };
    
            _conjureKit.OnLeft += (session) =>
            {
                sessionID.text = "";
            };
    
            _conjureKit.OnEntityAdded += CreateRaccoon;
            _conjureKit.Connect();
            
            
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

            handLandmarksPositions = new Vector3[HandTracker.LandmarksCount];

            _handTracker.OnUpdate += (landmarks, translations, isRightHand, score) =>
            {
                for (int h = 0; h < NumberOfTrackedHands; ++h)
                {
                    if (score[h] > 0)
                    {
                        var handPosition = new Vector3(
                            translations[h * 3 + 0], 
                            translations[h * 3 + 1],
                            translations[h * 3 + 2]);

                        var handLandmarkIndex = h * HandTracker.LandmarksCount * 3;
                        fingertipLandmark.transform.localPosition = handPosition + handLandmarksPositions[8];
                        
                        for (int l = 0; l < HandTracker.LandmarksCount; ++l)
                        {
                            handLandmarksPositions[l] = new Vector3(
                                landmarks[handLandmarkIndex + (l * 3) + 0],
                                landmarks[handLandmarkIndex + (l * 3) + 1],
                                landmarks[handLandmarkIndex + (l * 3) + 2]);
                    
                            // Update the landmarks position 
                            _handLandmarks[l].transform.localPosition = handPosition + handLandmarksPositions[l];
                        }
                        
                        var indexPalmDistance = Vector3.Distance(handLandmarksPositions[8], handLandmarksPositions[0]);
                        var middlePalmDistance = Vector3.Distance(handLandmarksPositions[12], handLandmarksPositions[0]);
                        var ringPalmDistance = Vector3.Distance(handLandmarksPositions[16], handLandmarksPositions[0]);
                        var pinkyPalmDistance = Vector3.Distance(handLandmarksPositions[20], handLandmarksPositions[0]);
                        
                        if (indexPalmDistance > 0.1f && middlePalmDistance < 0.08f && ringPalmDistance < 0.08f && pinkyPalmDistance < 0.08f)
                        {
                            if (!hasPlayedDead)
                            {
                                PlayDead();
                            }
                        }
                    }
                }
            };

            fingertipLandmark.transform.parent = arCamera.transform;
            _handTracker.Start(NumberOfTrackedHands);
        }

        private void Update()
        {
            _handTracker.Update();
            FeedMannaWithVideoFrames();
        }
        
        private void FeedMannaWithVideoFrames()
        {
            var imageAcquired = arCameraManager.TryAcquireLatestCpuImage(out var cpuImage);
            if (!imageAcquired)
            {
                AukiDebug.LogInfo("Couldn't acquire CPU image");
                return;
            }

            if (_videoTexture == null) _videoTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.R8, false);

            var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R8);
            cpuImage.ConvertAsync(
                conversionParams,
                (status, @params, buffer) =>
                {
                    _videoTexture.SetPixelData(buffer, 0, 0);
                    _videoTexture.Apply();
                    cpuImage.Dispose();

                    _manna.ProcessVideoFrameTexture(
                        _videoTexture,
                        arCamera.projectionMatrix,
                        arCamera.worldToCameraMatrix
                    );
                }
            );
        }
        
        private void ToggleControlsState(bool interactable)
        {
            if (spawnButton) spawnButton.interactable = interactable;
            if (qrCodeButton) qrCodeButton.interactable = interactable;
        }

        public void ToggleLighthouse()
        {
            qrCodeBool = !qrCodeBool;
            _manna.SetLighthouseVisible(qrCodeBool);
        }

        public void CreateRaccoonEntity()
        {
            if (_conjureKit.GetState() != State.Calibrated)
                return;
            
            Ray ray = arCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                Quaternion rotation = Quaternion.Euler(0, 180, 0);
                Pose hitPose = new Pose(hits[0].pose.position, rotation);

                _conjureKit.GetSession().AddEntity(
                    hitPose,
                    onComplete: entity => CreateRaccoon(entity),
                    onError: error => Debug.Log(error));
            }
        }

        private void CreateRaccoon(Entity entity)
        {
            if (entity.Flag == EntityFlag.EntityFlagParticipantEntity) return;

            var pose = _conjureKit.GetSession().GetEntityPose(entity);
            Instantiate(raccoon, pose.position, pose.rotation);
            GameObject.Find("SpawnButton").SetActive(false); // Remove spawn button
        }

        public void PlayDead()
        {
            hasPlayedDead = true;
            raccoonObject = GameObject.Find("Raccoon Cub PA(Clone)");
            raccoonAnimator = raccoonObject.GetComponent<Animator>();
            raccoonAnimator.SetTrigger("PlayDead");
        }
        
        public void GetUp()
        {
            hasPlayedDead = false;
            raccoonObject = GameObject.Find("Raccoon Cub PA(Clone)");
            raccoonAnimator = raccoonObject.GetComponent<Animator>();
            raccoonAnimator.SetTrigger("GetUp");
        }
    }
}