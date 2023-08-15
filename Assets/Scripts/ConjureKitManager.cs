using UnityEngine;
using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using Auki.Util;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ConjureKitManager : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    
    [SerializeField] private Text sessionState;
    [SerializeField] private Text sessionID;
    
    [SerializeField] private GameObject cube;
    [SerializeField] private Button spawnButton;
    
    [SerializeField] bool qrCodeBool;
    [SerializeField] Button qrCodeButton;
    
    private IConjureKit _conjureKit;
    private Manna _manna;
    
    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;
    
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
             sessionState.text = state.ToString();
             ToggleControlsState(state == State.Calibrated);
         };
    
         _conjureKit.OnJoined += session =>
         {
             sessionID.text = session.Id.ToString();
         };
    
         _conjureKit.OnLeft += () =>
         {
             sessionID.text = "";
         };
    
        _conjureKit.OnEntityAdded += CreateCube;
        
        _conjureKit.Init(ConjureKitConfiguration.Get());
    
        _conjureKit.Connect();
    }

    private void Update()
    {
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