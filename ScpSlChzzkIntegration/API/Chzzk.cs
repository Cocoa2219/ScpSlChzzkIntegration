using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Exiled.API.Features;
using MEC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

namespace ScpSlChzzkIntegration.API;

[Serializable]
public class Profile
{
    public string userIdHash;
    public string nickname;
    public string profileImageUrl;
    public string userRoleCode;
    public string badge;
    public string title;
    public string verifiedMark;
    public List<string> activityBadges;
    public StreamingProperty streamingProperty;

    [Serializable]
    public class StreamingProperty;
}

[Serializable]
public class DonationExtras
{
    private object emojis;
    public bool isAnonymous;
    public string payType;
    public int payAmount;
    public string streamingChannelId;
    public string nickname;
    public string osType;
    public string donationType;

    public List<WeeklyRank> weeklyRankList;

    [Serializable]
    public class WeeklyRank
    {
        public string userIdHash;
        public string nickName;
        public bool verifiedMark;
        public int donationAmount;
        public int ranking;
    }

    public WeeklyRank donationUserWeeklyRank;
}

[Serializable]
public class ChannelInfo
{
    public int code;
    public string message;
    public Content content;

    [Serializable]
    public class Content
    {
        public string channelId;
        public string channelName;
        public string channelImageUrl;
        public bool verifiedMark;
        public string channelType;
        public string channelDescription;
        public int followerCount;
        public bool openLive;
    }
}

[Serializable]
public class LiveStatus
{
    public int code;
    public string message;
    public Content content;

    [Serializable]
    public class Content
    {
        public string liveTitle;
        public string status;
        public int concurrentUserCount;
        public int accumulateCount;
        public bool paidPromotion;
        public bool adult;
        public string chatChannelId;
        public string categoryType;
        public string liveCategory;
        public string liveCategoryValue;
        public string livePollingStatusJson;
        public string faultStatus;
        public string userAdultStatus;
        public bool chatActive;
        public string chatAvailableGroup;
        public string chatAvailableCondition;
        public int minFollowerMinute;
    }
}

[Serializable]
public class AccessTokenResult
{
    public int code;
    public string message;
    public Content content;

    [Serializable]
    public class Content
    {
        public string accessToken;

        [Serializable]
        public class TemporaryRestrict
        {
            public bool temporaryRestrict;
            public int times;
            public int duration;
            public int createdTime;
        }

        public bool realNameAuth;
        public string extraToken;
    }
}

// https://github.com/JoKangHyeon/ChzzkUnity/blob/main/ChzzkUnity.cs 플러그인으로 포팅
public class Chzzk
{
    [Flags]
    private enum SslProtocolsHack
    {
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072
    }

    private string _cid;
    private string _token;
    private string _channel;

    private WebSocket _socket;
    private const string WsURL = "wss://kr-ss3.chat.naver.com/chat";

    private bool _running;

    private const string HeartbeatRequest = "{\"ver\":\"2\",\"cmd\":0}";
    private const string HeartbeatResponse = "{\"ver\":\"2\",\"cmd\":10000}";

    public Action<Profile, string> OnMessage = (_, _) => { };
    public Action<Profile, string, DonationExtras> OnDonation = (_, _, _) => { };

    private CoroutineHandle _heartbeatCoroutine;

    public void ConnectChzzk(string channelId)
    {
        Log.Debug("[치지직] 연결을 시작합니다.");

        _channel = channelId;

        if (_channel == "YOUR_CHANNEL_ID")
        {
            Log.Warn("[치지직] 채널 ID가 설정되지 않았습니다. 설정 파일을 확인해주세요.");
        }

        Timing.RunCoroutine(InternalConnect());
        _heartbeatCoroutine = Timing.RunCoroutine(Heartbeat());
    }

    public void StopListening()
    {
        Log.Debug("[치지직] 연결을 종료합니다.");

        Timing.KillCoroutines(_heartbeatCoroutine);

        _socket.Close();
        _socket = null;
    }

    private IEnumerator<float> InternalConnect()
    {
        if (_socket is { IsAlive: true })
        {
            _socket.Close();
            _socket = null;
        }

        LiveStatus liveStatus = null;

        yield return Timing.WaitUntilDone(GetLiveStatus(_channel, status => liveStatus = status));

        try
        {
            _cid = liveStatus.content.chatChannelId;
        }
        catch (Exception)
        {
            Log.Error("치지직 연결에 실패했습니다. 채널 정보를 가져오는 데 실패했습니다. 채널 ID를 확인해주세요.");
            yield break;
        }

        if (liveStatus != null && liveStatus.content.status != "OPEN")
        {
            Log.Warn("[치지직] 채널이 라이브 상태가 아닙니다. 올바른 채널 ID인지, 또는 라이브 상태인지 확인해주세요.");
        }

        if (liveStatus != null)
        {
            Log.Debug($"\n[치지직] 라이브 제목 : {liveStatus.content.liveTitle}\n현 시청자 : {liveStatus.content.concurrentUserCount}명");
        }

        AccessTokenResult accessTokenResult = null;

        yield return Timing.WaitUntilDone(GetAccessToken(_cid, t => accessTokenResult = t));

        try
        {
            _token = accessTokenResult.content.accessToken;
        }
        catch (Exception)
        {
            Log.Error("치지직 연결에 실패했습니다. 액세스 토큰을 가져오는 데 실패했습니다. 채널 ID를 확인해주세요.");
            yield break;
        }

        _socket = new WebSocket(WsURL);
        const SslProtocols sslProtocolHack =
            (SslProtocols)(SslProtocolsHack.Tls12 | SslProtocolsHack.Tls11 | SslProtocolsHack.Tls);
        _socket.SslConfiguration.EnabledSslProtocols = sslProtocolHack;

        _socket.OnMessage += Received;
        _socket.OnClose += CloseConnect;
        _socket.OnOpen += OnStartChat;

        _socket.Connect();
    }

    private void Received(object sender, MessageEventArgs ev)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<IDictionary<string, object>>(ev.Data);

            switch ((long)data["cmd"])
            {
                case 0:
                    _socket.Send(HeartbeatResponse);
                    break;
                case 93101:
                    var bdy = (JArray)data["bdy"];
                    var bdyObject = (JObject)bdy[0];

                    var profileText = bdyObject["profile"]!.ToString();
                    profileText = profileText.Replace("\\", "");
                    var profile = JsonUtility.FromJson<Profile>(profileText);

                    OnMessage(profile, bdyObject["msg"]!.ToString().Trim());
                    break;
                case 93102:
                    bdy = (JArray)data["bdy"];
                    bdyObject = (JObject)bdy[0];

                    if (bdyObject["profile"] != null)
                    {
                        profileText = bdyObject["profile"].ToString();
                        profileText = profileText.Replace("\\", "");
                        profile = JsonUtility.FromJson<Profile>(profileText);
                    }
                    else
                    {
                        profile = null;
                    }

                    var extraText = bdyObject["extras"]!.ToString();
                    extraText = extraText.Replace("\\", "");
                    var extras = JsonUtility.FromJson<DonationExtras>(extraText);

                    OnDonation(profile, bdyObject["msg"]!.ToString(), extras);
                    break;
                case 94008:
                case 94201:
                case 10000:
                case 10100:
                    break;
                default:
                    Log.Error($"알 수 없는 커맨드입니다. 제작자에게 빠르게 문의해주세요. ({data["cmd"]})");
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            throw;
        }
    }

    private void CloseConnect(object sender, CloseEventArgs ev)
    {
        Log.Debug($"[치지직] 치지직 연결이 종료되었습니다. 코드: {ev.Code}, 이유: {ev.Reason}");

        try
        {
            if (_socket == null) return;

            if (_socket.IsAlive) _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    private void OnStartChat(object sender, EventArgs ev)
    {
        Log.Debug("[치지직] 치지직에 연결되었습니다.");

        var message = $"{{\"ver\":\"2\",\"cmd\":100,\"svcid\":\"game\",\"cid\":\"{_cid}\",\"bdy\":{{\"uid\":null,\"devType\":2001,\"accTkn\":\"{_token}\",\"auth\":\"READ\"}},\"tid\":1}}";
        _running = true;
        _socket.Send(message);
    }

    public void RemoveAllOnMessageListener()
    {
        OnMessage = (_, _) => { };
    }

    public void RemoveAllOnDonationListener()
    {
        OnDonation = (_, _, _) => { };
    }

    private IEnumerator<float> Heartbeat()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(15f);

            if (!_running) continue;
            if (_socket is { IsAlive: true }) _socket.Send(HeartbeatRequest);

            Log.Debug("[치지직] ------------ Heartbeat 전송 ------------");
        }
    }

    private IEnumerator<float> GetChannelInfo(string channelId, Action<ChannelInfo> callback)
    {
        var url = $"https://api.chzzk.naver.com/service/v1/channels/{channelId}";
        var request = UnityWebRequest.Get(url);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        ChannelInfo channelInfo = null;

        Log.Debug($"[치지직] 채널 정보 요청 중... ({channelId}) - {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
            channelInfo = JsonConvert.DeserializeObject<ChannelInfo>(request.downloadHandler.text);

        Log.Debug($"[치지직] 채널 정보: {channelInfo!.content.channelName}");

        callback(channelInfo);
    }

    private IEnumerator<float> GetLiveStatus(string channelId, Action<LiveStatus> callback)
    {
        var url = $"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status";
        var request = UnityWebRequest.Get(url);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        LiveStatus liveStatus = null;

        Log.Debug($"[치지직] 라이브 상태 요청 중... ({channelId}) - {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
            liveStatus = JsonConvert.DeserializeObject<LiveStatus>(request.downloadHandler.text);

        Log.Debug($"[치지직] 라이브 상태: {liveStatus!.content.status}");

        callback(liveStatus);
    }

    private IEnumerator<float> GetAccessToken(string channelId, Action<AccessTokenResult> callback)
    {
        var url = $"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={channelId}&chatType=STREAMING";
        var request = UnityWebRequest.Get(url);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        Log.Debug($"[치지직] 액세스 토큰 요청 중... ({channelId}) - {request.result}");

        AccessTokenResult accessTokenResult = null;
        if (request.result == UnityWebRequest.Result.Success)
             accessTokenResult = JsonConvert.DeserializeObject<AccessTokenResult>(request.downloadHandler.text);

        Log.Debug($"[치지직] 액세스 토큰: {accessTokenResult!.content.accessToken}");

        callback(accessTokenResult);
    }

    private void Reconnect(string channelId)
    {

    }
}