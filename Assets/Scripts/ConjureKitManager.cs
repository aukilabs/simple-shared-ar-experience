using UnityEngine;
using Auki.ConjureKit;
using UnityEngine.UI;
using Auki.ConjureKit.Manna;
using Auki.Ur;
using Auki.Util;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ConjureKitManager : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARRaycastManager arRaycastManager;

    [SerializeField] private Text sessionState;
    [SerializeField] private Text sessionID;
    
    [SerializeField] private GameObject cube;
    [SerializeField] private Button spawnButton;

    [SerializeField] Button qrCodeButton;
    private bool qrCodeBool;
    
    private IConjureKit _conjureKit;
    private Manna _manna;
    
    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;
    
    [SerializeField] private Renderer fingertipLandmark;
    private HandTracker _handTracker;
    private bool landmarksVisualizeBool = true;
    
    [SerializeField] private AROcclusionManager arOcclusionManager;
    private bool occlusionBool = true;

    void Start()
    {
        arCameraManager = arCamera.GetComponent<ARCameraManager>();
        
        _conjureKit = new ConjureKit(
            arCamera.transform,
            "YOUR_APP_KEY",
            "YOUR_APP_SECRET");

        _manna = new Manna(_conjureKit);
        
        _conjureKit.OnStateChanged += state =>
        {
            if (state == State.JoinedSession)
            {
                Debug.Log("State.JoinedSession  " + Time.realtimeSinceStartup);
            }

            if (state == State.Calibrated)
            {
                Debug.Log("State.Calibrated  " + Time.realtimeSinceStartup);
            }

            sessionState.text = state.ToString();
            ToggleControlsState(state == State.Calibrated);
        };

        _conjureKit.OnJoined += session =>
        {
            Debug.Log("OnJoined " + Time.realtimeSinceStartup);
            sessionID.text = session.Id.ToString();
        };

        _conjureKit.OnLeft += () =>
        {
            sessionID.text = "";
        };

        _conjureKit.OnEntityAdded += CreateCube;
        _conjureKit.Connect();
        
        _handTracker = HandTracker.GetInstance();
        _handTracker.SetARSystem(arSession, arCamera, arRaycastManager);
        
        _handTracker.OnUpdate += (landmarks, translations, isRightHand, score) =>
        {
            if (score[0] > 0)
            {
                var handPosition = new Vector3(
                    translations[0],
                    translations[1],
                    translations[2]);

                var pointerLandmarkIndex = 8 * 3; // Index fingertip
                var pointerLandMarkPosition = new Vector3(
                    landmarks[pointerLandmarkIndex + 0],
                    landmarks[pointerLandmarkIndex + 1],
                    landmarks[pointerLandmarkIndex + 2]);

                fingertipLandmark.enabled = true;
                fingertipLandmark.transform.localPosition = handPosition + pointerLandMarkPosition;
            }
            else
            {
                fingertipLandmark.enabled = false;
            }
        };
        
        _handTracker.Start();
        _handTracker.ShowHandMesh();
    }
    
    private void Update()
    {
        FeedMannaWithVideoFrames();
        _handTracker.Update();
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
    
    public void ToggleHandLandmarks()
    {
        landmarksVisualizeBool = !landmarksVisualizeBool;

        if (landmarksVisualizeBool)
        {
            _handTracker.ShowHandMesh();
        }
        else
        {
            _handTracker.HideHandMesh();
        }
    }
    
    public void ToggleOcclusion()
    {
        occlusionBool = !occlusionBool;

        arOcclusionManager.requestedHumanDepthMode = occlusionBool ? HumanSegmentationDepthMode.Fastest : HumanSegmentationDepthMode.Disabled;
        arOcclusionManager.requestedHumanStencilMode = occlusionBool ? HumanSegmentationStencilMode.Fastest : HumanSegmentationStencilMode.Disabled;
        arOcclusionManager.requestedEnvironmentDepthMode = occlusionBool ? EnvironmentDepthMode.Fastest : EnvironmentDepthMode.Disabled;
    }

    public void CreateCubeEntity()
    {
        if (_conjureKit.GetState() != State.Calibrated)
            return;

        Vector3 position = arCamera.transform.position + arCamera.transform.forward * 0.5f;
        Quaternion rotation = Quaternion.Euler(0, arCamera.transform.eulerAngles.y, 0);

        Pose entityPos = new Pose(position, rotation);

        _conjureKit.GetSession().AddEntity(
            entityPos,
            onComplete: entity => CreateCube(entity),
            onError: error => Debug.Log(error));
    }

    private void CreateCube(Entity entity)
    {
        if (entity.Flag == EntityFlag.EntityFlagParticipantEntity) return;

        var pose = _conjureKit.GetSession().GetEntityPose(entity);
        Instantiate(cube, pose.position, pose.rotation);
    }
}