namespace Miko.Components.Webcam;

/// <summary>The texts of <see cref="MikoWebcam"/>. English by default; <see cref="Chinese"/> is built in.</summary>
public sealed record WebcamTexts
{
    public string Connecting { get; init; } = "Connecting…";
    public string Retry { get; init; } = "Retry";
    public string Smooth { get; init; } = "SD";
    public string High { get; init; } = "HD";
    public string Snapshot { get; init; } = "Snapshot";
    public string Privacy { get; init; } = "Privacy";
    public string Flip { get; init; } = "Flip";
    public string Fullscreen { get; init; } = "Fullscreen";
    public string PrivacyOn { get; init; } = "Privacy mode is on";
    public string TurnOffPrivacy { get; init; } = "Turn off";
    public string LocalNetwork { get; init; } = "LAN";
    public string CloudRelay { get; init; } = "Cloud";
    public string PtzHint { get; init; } = "Hold an arrow to turn the camera";
    public string PtzUnavailable { get; init; } = "Pan / tilt needs the camera online in the cloud";
    public string SnapshotTaken { get; init; } = "Snapshot taken";
    public string Flipped { get; init; } = "Picture flipped";

    /// <summary>Shown after switching quality; <c>{0}</c> is the quality's name.</summary>
    public string QualityChanged { get; init; } = "Switched to {0}";

    // Failures the view explains itself; others show the provider's message.
    public string NotOnNetwork { get; init; } = "The camera is not on this network.";
    public string Offline { get; init; } = "The camera is offline.";
    public string VerifyCodeNeeded { get; init; } = "Enter the camera's verification code to watch it on this network.";
    public string ViewerLimit { get; init; } = "The account's limit of simultaneous viewers is reached.";
    public string WrongVerifyCode { get; init; } = "The verification code is wrong.";
    public string PtzLimit { get; init; } = "The camera cannot turn further this way.";

    /// <summary>The texts in Simplified Chinese.</summary>
    public static WebcamTexts Chinese { get; } = new()
    {
        Connecting = "正在连接…",
        Retry = "重试",
        Smooth = "流畅",
        High = "高清",
        Snapshot = "截图",
        Privacy = "隐私遮蔽",
        Flip = "画面翻转",
        Fullscreen = "全屏",
        PrivacyOn = "隐私遮蔽已开启",
        TurnOffPrivacy = "关闭遮蔽",
        LocalNetwork = "局域网直连",
        CloudRelay = "云端转发",
        PtzHint = "按住方向键转动摄像头",
        PtzUnavailable = "设备在云端离线，暂不能控制云台",
        SnapshotTaken = "截图成功",
        Flipped = "画面已翻转",
        QualityChanged = "已切换为{0}",
        NotOnNetwork = "摄像头不在当前网络中",
        Offline = "设备不在线",
        VerifyCodeNeeded = "请输入设备验证码后在局域网内观看",
        ViewerLimit = "同时观看人数已达账号上限",
        WrongVerifyCode = "设备验证码错误",
        PtzLimit = "云台已转到尽头",
    };

    /// <summary>The text shown for a failure: one of the texts above for the failures it knows, else the exception's message.</summary>
    public string Describe(Exception error) => error switch
    {
        WebcamException { Code: "unreachable" } => NotOnNetwork,
        WebcamException { Code: "20007" } => Offline,
        WebcamException { Code: "verifycode" } => VerifyCodeNeeded,
        WebcamException { Code: "9048" or "9049" or "399048" or "399049" } => ViewerLimit,
        WebcamException { Code: "60002" or "60003" or "60004" or "60005" } => PtzLimit,
        Streaming.RtspException { StatusCode: 401 } => WrongVerifyCode,
        HttpRequestException => "The camera service could not be reached.",
        _ => error.Message,
    };

    internal string QualityName(WebcamQuality quality) => quality == WebcamQuality.High ? High : Smooth;
}
