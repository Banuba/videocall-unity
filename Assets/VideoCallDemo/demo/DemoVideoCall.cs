using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Agora.Rtc;
using Agora.Util;
using Logger = Agora.Util.Logger;

#if UNITY_2018_1_OR_NEWER
using Unity.Collections;
#endif


public class DemoVideoCall : MonoBehaviour 
{
    [Header("_____________Basic Configuration_____________")]
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
    private Texture2D _bufferTexture;


    // Use this for initialization
    private void Start()
    {
        if (CheckAppId())
        {
            InitTextureIfRequired();
            InitEngine();
            SetExternalVideoSource();
            JoinChannel();
        }
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

        if (_bufferTexture != null && (_bufferTexture.width != _renderToTexture.texture.width || _bufferTexture.height != _renderToTexture.texture.height))
        {
            Destroy(_bufferTexture);
            _bufferTexture = new Texture2D(_renderToTexture.texture.width, _renderToTexture.texture.height, TextureFormat.RGBA32, false);
            return;
        }
        _bufferTexture = new Texture2D(_renderToTexture.texture.width, _renderToTexture.texture.height, TextureFormat.RGBA32, false);
    }

    private void Update()
    {
        PermissionHelper.RequestMicrophontPermission();
        StartCoroutine(SendVideoFrame());
        if(_renderToTexture != null)
        {
            _localCameraView.texture = _renderToTexture.texture;
        }

    }

    private byte[] GetTextureData(RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;
        _bufferTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        _bufferTexture.Apply();
        RenderTexture.active = null;
        return _bufferTexture.GetRawTextureData();
    }

    private IEnumerator SendVideoFrame()
    {
        yield return new WaitForEndOfFrame();
        InitTextureIfRequired();
        IRtcEngine rtc = Agora.Rtc.RtcEngine.Instance;
        if (rtc == null) yield break;

        byte[] bytes = GetTextureData(_renderToTexture.texture);
        ExternalVideoFrame externalVideoFrame = new ExternalVideoFrame
        {
            type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
            format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA,
            buffer = bytes,
            stride = _renderToTexture.texture.width,
            height = _renderToTexture.texture.height,
            cropLeft = 0,
            cropTop = 0,
            cropRight = 0,
            cropBottom = 0,
            rotation = 180,
            timestamp = System.DateTime.Now.Ticks / 10000
    };
        var result = rtc.PushVideoFrame(externalVideoFrame);
        if (result != 0)
        {
            Debug.Log("PushVideoFrame failure");
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
        Log = new Logger(LogText);
        return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in Canvas!!!!");
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
        go.AddComponent<UIElementDrag>();
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
        _customCaptureVideo.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
    }

    public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
    {
        int build = 0;
        _customCaptureVideo.Log.UpdateLog(string.Format("sdk version: ${0}",
            _customCaptureVideo.RtcEngine.GetVersion(ref build)));
        _customCaptureVideo.Log.UpdateLog(
            string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                connection.channelId, connection.localUid, elapsed));

        //DemoVideoCall.MakeVideoView(0);
    }

    public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
    {
        _customCaptureVideo.Log.UpdateLog("OnRejoinChannelSuccess");
    }

    public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
    {
        _customCaptureVideo.Log.UpdateLog("OnLeaveChannel");
    }

    public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole,
        CLIENT_ROLE_TYPE newRole)
    {
        _customCaptureVideo.Log.UpdateLog("OnClientRoleChanged");
    }

    public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
    {
        _customCaptureVideo.Log.UpdateLog(
            string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
        DemoVideoCall.MakeVideoView(uid, _customCaptureVideo.GetChannelName());
    }

    public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
    {
        _customCaptureVideo.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
            (int)reason));
        DemoVideoCall.DestroyVideoView(uid);
    }
}

#endregion
