using System.Collections;
using UnityEngine;
using agora_gaming_rtc;
using UnityEngine.UI;
using AgoraUtilities;

public class DemoVideoCall : MonoBehaviour 
{
    private const string _channelName = "Agora_Channel";
    private const float _offset = 100;
    
    [SerializeField]
    private string APP_ID = "YOUR_APPID";
    [SerializeField]
    private string TOKEN = "";
    [SerializeField]
    private string CHANNEL_NAME = "YOUR_CHANNEL_NAME";
    
    [SerializeField]
    private Text _logText;
    [SerializeField]
    private RawImage _localCameraImage;
    [SerializeField]
    private Transform _layout;
    [SerializeField]
    private BNB.RenderToTexture _renderToTexture;
    [SerializeField] 
    private Vector2 _cameraViewSize;

    private int _timestamp;
    private Logger _logger;
    private IRtcEngine _mRtcEngine;
    private Texture2D _bufferTexture;

    private void Start ()
    {
        InitTexture();
		CheckAppId();	
		InitEngine();
		JoinChannel();
        PermissionHelper.RequestMicrophontPermission();
    }
    
    private void Update()
    {
        StartCoroutine(SendVideoFrame());
    }

    private IEnumerator SendVideoFrame()
    {
        yield return new WaitForEndOfFrame();
        IRtcEngine rtc = IRtcEngine.QueryEngine();
        if (rtc == null) yield break;
        
        byte[] bytes = GetTextureData(_renderToTexture.texture);
        ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame
        {
            type = ExternalVideoFrame.VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
            format = ExternalVideoFrame.VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_ARGB,
            buffer = bytes,
            stride = _renderToTexture.texture.width,
            height = _renderToTexture.texture.height,
            cropLeft = 0,
            cropTop = 0,
            cropRight = 0,
            cropBottom = 0,
            rotation = 180,
            timestamp = _timestamp++
        };
        var result = rtc.PushVideoFrame(externalVideoFrame);
        if (result != 0)
        {
            Debug.Log("PushVideoFrame failure");
        }
    }

    private byte[] GetTextureData(RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;
        _bufferTexture.ReadPixels(new Rect(0, 0, _bufferTexture.width, _bufferTexture.height), 0, 0);
        _bufferTexture.Apply();
        RenderTexture.active = null;
        return _bufferTexture.GetRawTextureData();
    }

    private void InitTexture()
    {
        if(_renderToTexture == null)
        {
            Debug.LogWarning("Assign RenderToTexture");
        }

        if (_bufferTexture != null)
        {
            Destroy(_bufferTexture);
        }
        _bufferTexture = new Texture2D(_renderToTexture.texture.width, _renderToTexture.texture.height, TextureFormat.ARGB32, false);
        _localCameraImage.texture = _bufferTexture;
    }
    
    private void CheckAppId()
    {
        _logger = new Logger(_logText);
        _logger.DebugAssert(APP_ID.Length > 10, "Please fill in your appId in Canvas.");
    }

	private void InitEngine()
	{
        _mRtcEngine = IRtcEngine.GetEngine(APP_ID);
		_mRtcEngine.SetLogFile("log.txt");
		_mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
		_mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
		_mRtcEngine.EnableAudio();
		_mRtcEngine.EnableVideo();
		_mRtcEngine.EnableVideoObserver();
		_mRtcEngine.SetExternalVideoSource(true, false);
        _mRtcEngine.OnJoinChannelSuccess += OnJoinChannelSuccessHandler;
        _mRtcEngine.OnLeaveChannel += OnLeaveChannelHandler;
        _mRtcEngine.OnWarning += OnSDKWarningHandler;
        _mRtcEngine.OnError += OnSDKErrorHandler;
        _mRtcEngine.OnConnectionLost += OnConnectionLostHandler;
        _mRtcEngine.OnUserJoined += OnUserJoinedHandler;
        _mRtcEngine.OnUserOffline += OnUserOfflineHandler;
    }
    
    private void OnDestroy()
    {
        if (_mRtcEngine == null) return;
        _mRtcEngine.LeaveChannel();
        _mRtcEngine.DisableVideoObserver();
        _mRtcEngine.OnJoinChannelSuccess -= OnJoinChannelSuccessHandler;
        _mRtcEngine.OnLeaveChannel -= OnLeaveChannelHandler;
        _mRtcEngine.OnWarning -= OnSDKWarningHandler;
        _mRtcEngine.OnError -= OnSDKErrorHandler;
        _mRtcEngine.OnConnectionLost -= OnConnectionLostHandler;
        _mRtcEngine.OnUserJoined -= OnUserJoinedHandler;
        _mRtcEngine.OnUserOffline -= OnUserOfflineHandler;
        IRtcEngine.Destroy();
        _mRtcEngine = null;
    }

    private void JoinChannel()
	{
        int ret = _mRtcEngine.JoinChannelByKey(TOKEN, CHANNEL_NAME, "", 0);
        Debug.Log($"JoinChannel ret: ${ret}");
	}
    
	private void OnJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        _logger.UpdateLog($"SDK version: ${IRtcEngine.GetSdkVersion()}");
        _logger.UpdateLog($"onJoinChannelSuccess channelName: {channelName}, uid: {uid}, elapsed: {elapsed}");
    }

    private void OnLeaveChannelHandler(RtcStats stats)
    {
        _logger.UpdateLog("OnLeaveChannelSuccess");
    }

    private void OnUserJoinedHandler(uint uid, int elapsed)
    {
        _logger.UpdateLog($"OnUserJoined uid: ${uid} elapsed: ${elapsed}");
        MakeVideoView(uid);
    }

    private void OnUserOfflineHandler(uint uid, USER_OFFLINE_REASON reason)
    {
        _logger.UpdateLog($"OnUserOffLine uid: ${uid}, reason: ${(int)reason}");
        DestroyVideoView(uid);
    }

    private void OnSDKWarningHandler(int warn, string msg)
    {
        _logger.UpdateLog($"OnSDKWarning warn: {warn}, msg: {msg}");
    }
    
    private void OnSDKErrorHandler(int error, string msg)
    {
        _logger.UpdateLog($"OnSDKError error: {error}, msg: {msg}");
    }
    
    private void OnConnectionLostHandler()
    {
        _logger.UpdateLog("OnConnectionLost ");
    }

    public void OnMuteToggle(bool toggleState)
    {
        _mRtcEngine.MuteLocalAudioStream(toggleState);
    }

    private void DestroyVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (go != null)
        {
            Destroy(go);
        }
    }

    private void MakeVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (go != null)
        {
            return; // reuse
        }
        // configure videoSurface
        VideoSurface videoSurface = CreateVideoSurface(uid.ToString());
        videoSurface.SetForUser(uid);
        videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
        videoSurface.videoFps = 30;
    }

    private VideoSurface CreateVideoSurface(string uid)
    {
        GameObject go = new GameObject
        {
            name = uid
        };
        RawImage imageComponent = go.AddComponent<RawImage>();
        imageComponent.rectTransform.sizeDelta = _cameraViewSize;
        go.AddComponent<UIElementDrag>();
        
        go.transform.SetParent(_layout);
        go.transform.Rotate(0f, 0.0f, 180.0f);
        go.transform.localScale = Vector3.one;
        
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }
}
