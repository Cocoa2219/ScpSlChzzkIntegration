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
    public class StreamingProperty
    {
    }
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

    private string cid;
    private string token;
    private string channel;

    private WebSocket socket;
    private readonly string wsURL = "wss://kr-ss3.chat.naver.com/chat";

    private bool running = false;

    private const string heartbeatRequest = "{\"ver\":\"2\",\"cmd\":0}";
    private const string heartbeatResponse = "{\"ver\":\"2\",\"cmd\":10000}";

    public Action<Profile, string> onMessage = (profile, str) => { };
    public Action<Profile, string, DonationExtras> onDonation = (profile, str, extra) => { };

    public void ConnectChzzk(string channelId)
    {
        Log.Debug("치지직 연결을 시도합니다...");

        channel = channelId;

        Timing.RunCoroutine(InternalConnect());
        Timing.RunCoroutine(Heartbeat());
    }

    public void StopListening()
    {
        Log.Debug("치지직 연결을 종료합니다.");

        socket.Close();
        socket = null;
    }

    private IEnumerator<float> InternalConnect()
    {
        if (socket is { IsAlive: true })
        {
            socket.Close();
            socket = null;
        }

        LiveStatus liveStatus = null;

        yield return Timing.WaitUntilDone(GetLiveStatus(channel, status => liveStatus = status));

        try
        {
            cid = liveStatus.content.chatChannelId;
        }
        catch (Exception e)
        {
            Log.Error("치지직 연결에 실패했습니다. 채널 정보를 가져오는 데 실패했습니다. 채널 ID를 확인해주세요.");
            Log.Error(e);
            throw;
        }

        AccessTokenResult accessTokenResult = null;

        yield return Timing.WaitUntilDone(GetAccessToken(cid, t => accessTokenResult = t));

        try
        {
            token = accessTokenResult.content.accessToken;

            socket = new WebSocket(wsURL);
            const SslProtocols sslProtocolHack =
                (SslProtocols)(SslProtocolsHack.Tls12 | SslProtocolsHack.Tls11 | SslProtocolsHack.Tls);
            socket.SslConfiguration.EnabledSslProtocols = sslProtocolHack;

            socket.OnMessage += Received;
            socket.OnClose += CloseConnect;
            socket.OnOpen += OnStartChat;

            socket.Connect();
        }
        catch (Exception e)
        {
            Log.Error("치지직 연결에 실패했습니다. 액세스 토큰을 가져오는 데 실패했습니다. 채널 ID를 확인해주세요.");
            Console.WriteLine(e);
            throw;
        }
    }

    private void Received(object sender, MessageEventArgs ev)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<IDictionary<string, object>>(ev.Data);

            switch ((long)data["cmd"])
            {
                case 0:
                    socket.Send(heartbeatResponse);
                    break;
                case 93101:
                    var bdy = (JArray)data["bdy"];
                    var bdyObject = (JObject)bdy[0];

                    var profileText = bdyObject["profile"]!.ToString();
                    profileText = profileText.Replace("\\", "");
                    var profile = JsonUtility.FromJson<Profile>(profileText);

                    onMessage(profile, bdyObject["msg"]!.ToString().Trim());
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

                    onDonation(profile, bdyObject["msg"]!.ToString(), extras);
                    break;
                case 94008:
                case 94201:
                case 10000:
                case 10100:
                    break;
                default:
                    Log.Debug($"알 수 없는 커맨드입니다. 제작자에게 빠르게 문의해주세요.\n{data["cmd"]}");
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
        Log.Debug($"치지직 연결이 종료되었습니다. 코드: {ev.Code}, 이유: {ev.Reason}");

        try
        {
            if (socket == null) return;

            if (socket.IsAlive) socket.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    private void OnStartChat(object sender, EventArgs ev)
    {
        Log.Debug("치지직에 연결되었습니다.");

        var message = $"{{\"ver\":\"2\",\"cmd\":100,\"svcid\":\"game\",\"cid\":\"{cid}\",\"bdy\":{{\"uid\":null,\"devType\":2001,\"accTkn\":\"{token}\",\"auth\":\"READ\"}},\"tid\":1}}";
        running = true;
        socket.Send(message);
    }

    public void RemoveAllOnMessageListener()
    {
        onMessage = (profile, str) => { };
    }

    public void RemoveAllOnDonationListener()
    {
        onDonation = (profile, str, extra) => { };
    }

    private IEnumerator<float> Heartbeat()
    {
        while (running)
        {
            if (socket is { IsAlive: true }) socket.Send(heartbeatRequest);

            Log.Debug("치지직에 하트비트를 전송했습니다.");

            yield return Timing.WaitForSeconds(15f);
        }
    }

    private IEnumerator<float> GetChannelInfo(string channelId, Action<ChannelInfo> callback)
    {
        var URL = $"https://api.chzzk.naver.com/service/v1/channels/{channelId}";
        var request = UnityWebRequest.Get(URL);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        ChannelInfo channelInfo = null;

        Log.Debug($"[치지직] 채널 정보 요청 중... {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
            channelInfo = JsonConvert.DeserializeObject<ChannelInfo>(request.downloadHandler.text);

        Log.Debug($"[치지직] 채널 정보: {channelInfo!.content.channelName}");

        callback(channelInfo);
    }

    private IEnumerator<float> GetLiveStatus(string channelId, Action<LiveStatus> callback)
    {
        var URL = $"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status";
        var request = UnityWebRequest.Get(URL);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        LiveStatus liveStatus = null;

        Log.Debug($"[치지직] 라이브 상태 요청 중... {request.result}");

        if (request.result == UnityWebRequest.Result.Success)
            liveStatus = JsonConvert.DeserializeObject<LiveStatus>(request.downloadHandler.text);

        Log.Debug($"[치지직] 라이브 상태: {liveStatus!.content.status}");

        callback(liveStatus);
    }

    private IEnumerator<float> GetAccessToken(string channelId, Action<AccessTokenResult> callback)
    {
        var URL = $"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={channelId}&chatType=STREAMING";
        var request = UnityWebRequest.Get(URL);

        yield return Timing.WaitUntilDone(request.SendWebRequest());

        Log.Debug($"[치지직] 액세스 토큰 요청 중... {request.result}");

        AccessTokenResult accessTokenResult = null;
        if (request.result == UnityWebRequest.Result.Success)
            accessTokenResult = JsonConvert.DeserializeObject<AccessTokenResult>(request.downloadHandler.text);

        Log.Debug($"[치지직] 액세스 토큰: {accessTokenResult!.content.accessToken}");

        callback(accessTokenResult);
    }
}