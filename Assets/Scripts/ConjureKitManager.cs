using System.Collections.Generic;
using UnityEngine;
using Auki.ConjureKit;
using UnityEngine.UI;
using Auki.ConjureKit.Manna;
using Auki.Ur;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using State = Auki.ConjureKit.State;

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
    private bool _qrCodeBool;

    private IConjureKit _conjureKit;
    private Manna _manna;

    private ARCameraManager _arCameraManager;
    private Texture2D _videoTexture;

    [SerializeField] private GameObject fingertipLandmark;
    private HandTracker _handTracker;
    private bool _landmarksVisualizeBool = false;

    [SerializeField] private AROcclusionManager arOcclusionManager;
    private bool _occlusionBool = true;

    [SerializeField] private Transform arSessionOrigin;

    private ColorSystem _colorSystem;
    private Dictionary<uint, Renderer> _cubes = new Dictionary<uint, Renderer>();

    void Start()
    {
        _arCameraManager = arCamera.GetComponent<ARCameraManager>();

        _conjureKit = new ConjureKit(
            arCamera.transform,
            "YOUR_APP_KEY",
            "YOUR_APP_SECRET");

        _manna = new Manna(_conjureKit);
        _manna.GetOrCreateFrameFeederComponent().AttachMannaInstance(_manna);

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

            _colorSystem = new ColorSystem(session);
            session.RegisterSystem(_colorSystem, () => Debug.Log("System registered in session"));
            _colorSystem.OnColorComponentUpdated += OnColorComponentUpdated;
        };

        _conjureKit.OnLeft += (Session session) =>
        {
            sessionID.text = "";
        };

        _conjureKit.OnEntityAdded += CreateCube;
        _conjureKit.Connect();

        _handTracker = HandTracker.GetInstance();
        _handTracker.SetARSystem(arSession, arCamera, arRaycastManager);

        _handTracker.OnUpdate += (landmarks, translations, isRightHand, score) =>
        {
            if (score[0] > 0 && _landmarksVisualizeBool)
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

                fingertipLandmark.SetActive(true);

                fingertipLandmark.transform.position =
                    arCamera.transform.TransformPoint(handPosition + pointerLandMarkPosition);
            }
            else
            {
                fingertipLandmark.SetActive(false);
            }
        };

        _handTracker.Start();
    }

    private void Update()
    {
        _handTracker.Update();
    }

    private void ToggleControlsState(bool interactable)
    {
        if (spawnButton) spawnButton.interactable = interactable;
        if (qrCodeButton) qrCodeButton.interactable = interactable;
    }

    public void ToggleLighthouse()
    {
        _qrCodeBool = !_qrCodeBool;
        _manna.SetLighthouseVisible(_qrCodeBool);
    }

    public void ToggleHandLandmarks()
    {
        _landmarksVisualizeBool = !_landmarksVisualizeBool;

        if (_landmarksVisualizeBool)
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
        _occlusionBool = !_occlusionBool;

        arOcclusionManager.requestedHumanDepthMode = _occlusionBool ? HumanSegmentationDepthMode.Fastest : HumanSegmentationDepthMode.Disabled;
        arOcclusionManager.requestedHumanStencilMode = _occlusionBool ? HumanSegmentationStencilMode.Fastest : HumanSegmentationStencilMode.Disabled;
        arOcclusionManager.requestedEnvironmentDepthMode = _occlusionBool ? EnvironmentDepthMode.Fastest : EnvironmentDepthMode.Disabled;
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
            onComplete: entity =>
            {
                // Initialize with white color
                _colorSystem.SetColor(entity.Id, Color.white);

                CreateCube(entity);
            },
            onError: error => Debug.Log(error));
    }

    private void CreateCube(Entity entity)
    {
        if (entity.Flag == EntityFlag.EntityFlagParticipantEntity) return;

        var pose = _conjureKit.GetSession().GetEntityPose(entity);
        var touchableCube = Instantiate(cube, pose.position, pose.rotation).GetComponent<TouchableByHand>();
        _cubes[entity.Id] = touchableCube.GetComponent<Renderer>();
        _cubes[entity.Id].material.color = _colorSystem.GetColor(entity.Id);

        touchableCube.OnTouched += () =>
        {
            _colorSystem.SetColor(entity.Id, Random.ColorHSV());
            _cubes[entity.Id].material.color = _colorSystem.GetColor(entity.Id);
        };
    }

    private void OnColorComponentUpdated(uint entityId, Color color)
    {
        _cubes[entityId].material.color = color;
    }
}