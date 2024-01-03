using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Agora.Rtc;
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif

#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
#endif


public class DemoVideoCall : MonoBehaviour 
{
    [Header("_____________Agora Configuration_____________")]
    [FormerlySerializedAs("APP_ID")]
    [SerializeField]
    private string _appID = "";

    [FormerlySerializedAs("TOKEN")]
    [SerializeField]
    private string _token = "";

    [FormerlySerializedAs("CHANNEL_NAME")]
    [SerializeField]
    private string _channelName = "";

    public Text LogText;
    internal Logger Log;
    internal IRtcEngine RtcEngine = null;

    [Header("_____________Banuba SDK Refs_____________")]
    [SerializeField]
    private RawImage _localCameraView;

    [SerializeField]
    private BNB.RenderToTexture _renderToTexture;
    private byte[] _shareData;
    private Texture2D _texture;
    private Rect _rect;


    // Use this for initialization
    private void Start()
    {
        if (CheckAppId())
        {
            InitTextureIfRequired();
            //InitTexture();
            InitEngine();
            SetExternalVideoSource();
            JoinChannel();
        }
    }

    private void InitTexture()
    {
        _rect = new UnityEngine.Rect(0, 0, Screen.width, Screen.height);
        _texture = new Texture2D((int)_rect.width, (int)_rect.height, TextureFormat.RGBA32, false);
    }

    private void InitTextureIfRequired()
    {
        if (_renderToTexture == null)
        {
            Debug.LogWarning("Assign RenderToTexture");
        }

        if (_renderToTexture.texture == null)
        {
            return;
        }

        if (_texture != null && (_texture.width != _renderToTexture.texture.width || _texture.height != _renderToTexture.texture.height))
        {
            Debug.Log("Recreeate");
            Destroy(_texture);
            _rect = new UnityEngine.Rect(0, 0, _renderToTexture.texture.width, _renderToTexture.texture.height);
            _texture = new Texture2D((int)_rect.width, (int)_rect.height, TextureFormat.RGBA32, false);
            return;
        }
        if(_texture == null)
        {
            _rect = new UnityEngine.Rect(0, 0, 1, 1);
            _texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        }
       
    }

    private void Update()
    {
        //request microphone
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
		{                 
			Permission.RequestUserPermission(Permission.Microphone);
		}
#endif
    StartCoroutine(SendVideoFrame());
        if(_renderToTexture != null)
        {
            _localCameraView.texture = _renderToTexture.texture;
        }

    }

    private IEnumerator SendVideoFrame()
    {
        yield return new WaitForEndOfFrame();
        IRtcEngine rtc = Agora.Rtc.RtcEngine.Instance;
        if (rtc != null && _renderToTexture.texture != null)
        {
            InitTextureIfRequired();
            RenderTexture.active = _renderToTexture.texture;
            _texture.ReadPixels(_rect, 0, 0);
            _texture.Apply();
            RenderTexture.active = null;

#if UNITY_2018_1_OR_NEWER
            NativeArray<byte> nativeByteArray = _texture.GetRawTextureData<byte>();
            if (_shareData?.Length != nativeByteArray.Length)
            {
                _shareData = new byte[nativeByteArray.Length];
            }
            nativeByteArray.CopyTo(_shareData);
#else
                _shareData = _texture.GetRawTextureData();
#endif

            ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame();
            externalVideoFrame.type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA;
            externalVideoFrame.format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA;
            externalVideoFrame.buffer = _shareData;
            externalVideoFrame.stride = (int)_rect.width;
            externalVideoFrame.height = (int)_rect.height;
            externalVideoFrame.cropLeft = 10;
            externalVideoFrame.cropTop = 10;
            externalVideoFrame.cropRight = 10;
            externalVideoFrame.cropBottom = 10;
            externalVideoFrame.rotation = 180;
            externalVideoFrame.timestamp = System.DateTime.Now.Ticks / 10000;
            var ret = rtc.PushVideoFrame(externalVideoFrame);
            Debug.Log("PushVideoFrame ret = " + ret + "time: " + System.DateTime.Now.Millisecond);
        }
    }

    private void InitEngine()
    {
        RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
        UserEventHandler handler = new UserEventHandler(this);
        RtcEngineContext context = new RtcEngineContext(_appID, 0,
            CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
            AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT);
        RtcEngine.Initialize(context);
        RtcEngine.InitEventHandler(handler);
    }

    private void SetExternalVideoSource()
    {
        var ret = RtcEngine.SetExternalVideoSource(true, false, EXTERNAL_VIDEO_SOURCE_TYPE.VIDEO_FRAME, new SenderOptions());
        Debug.Log("SetExternalVideoSource returns:" + ret);
    }

    private void JoinChannel()
    {
        RtcEngine.EnableAudio();
        RtcEngine.EnableVideo();
        RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        RtcEngine.JoinChannel(_token, _channelName);
    }

    private bool CheckAppId()
    {
        //Log = new Logger(LogText);
        //return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in Canvas!!!!");

        if(_appID.Length < 10)
        {
            Debug.LogError("Please fill in your appId in Canvas!!!!");
            return false;
        }
        return true;
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
        if (RtcEngine == null) return;
        RtcEngine.InitEventHandler(null);
        RtcEngine.LeaveChannel();
        RtcEngine.Dispose();
    }

    internal string GetChannelName()
    {
        return _channelName;
    }

    #region -- Video Render UI Logic ---

    internal static void MakeVideoView(uint uid, string channelId = "")
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            return; // reuse
        }

        // create a GameObject and assign to this new user
        VideoSurface videoSurface = makeImageSurface(uid.ToString());
        if (!ReferenceEquals(videoSurface, null))
        {
            // configure videoSurface
            if (uid == 0)
            {
                videoSurface.SetForUser(uid, channelId);
            }
            else
            {
                videoSurface.SetForUser(uid, channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            }

            videoSurface.OnTextureSizeModify += (int width, int height) =>
            {
                float scale = (float)height / (float)width;
                videoSurface.transform.localScale = new Vector3(-5, 5 * scale, 1);
                Debug.Log("OnTextureSizeModify: " + width + "  " + height);
            };

            videoSurface.SetEnable(true);
        }
    }

    // VIDEO TYPE 1: 3D Object
    private static VideoSurface MakePlaneSurface(string goName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // set up transform
        go.transform.Rotate(-90.0f, 0.0f, 0.0f);
        go.transform.position = Vector3.zero;
        go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    // Video TYPE 2: RawImage
    private static VideoSurface makeImageSurface(string goName)
    {
        GameObject go = new GameObject();

        if (go == null)
        {
            return null;
        }

        go.name = goName;
        // to be renderered onto
        go.AddComponent<RawImage>();
        // make the object draggable
        go.AddComponent<Demo.Util.UIElementDrag>();
        GameObject canvas = GameObject.Find("VideoCanvas");
        if (canvas != null)
        {
            go.transform.parent = canvas.transform;
            Debug.Log("add video view");
        }
        else
        {
            Debug.Log("Canvas is null video view");
        }

        // set up transform
        go.transform.Rotate(0f, 0.0f, 180.0f);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(3f, 4f, 1f);

        // configure videoSurface
        VideoSurface videoSurface = go.AddComponent<VideoSurface>();
        return videoSurface;
    }

    internal static void DestroyVideoView(uint uid)
    {
        GameObject go = GameObject.Find(uid.ToString());
        if (!ReferenceEquals(go, null))
        {
            Destroy(go);
        }
    }

    #endregion
}

#region -- Agora Event ---

internal class UserEventHandler : IRtcEngineEventHandler
{
    private readonly DemoVideoCall _customCaptureVideo;

    internal UserEventHandler(DemoVideoCall customCaptureVideo)
    {
        _customCaptureVideo = customCaptureVideo;
    }

    public override void OnError(int err, string msg)
    {
        Debug.Log(string.Format("OnError err: {0}, msg: {1}", err, msg));
    }

    public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
    {
        int build = 0;
        Debug.Log(string.Format("sdk version: ${0}",
            _customCaptureVideo.RtcEngine.GetVersion(ref build)));
        Debug.Log(
            string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                connection.channelId, connection.localUid, elapsed));

        //DemoVideoCall.MakeVideoView(0);
    }

    public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
    {
        Debug.Log("OnRejoinChannelSuccess");
    }

    public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
    {
        Debug.Log("OnLeaveChannel");
    }

    public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole,
        CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions)
    {
        Debug.Log("OnClientRoleChanged");
    }

    public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
    {
        Debug.Log(
            string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
        DemoVideoCall.MakeVideoView(uid, _customCaptureVideo.GetChannelName());
    }

    public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
    {
        Debug.Log(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
            (int)reason));
        DemoVideoCall.DestroyVideoView(uid);
    }
}

#endregion
