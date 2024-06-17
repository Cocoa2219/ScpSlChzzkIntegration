using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace ScpSlChzzkIntegration
{
    public class Config : IConfig
    {
        [Description("플러그인을 끄거나 켭니다.")]
        public bool IsEnabled { get; set; } = true;

        [Description("디버그 모드를 켜거나 끕니다.")]
        public bool Debug { get; set; } = false;

        [Description("치지직 채널의 ID를 입력합니다. 채널 페이지 접속 후 주소를 확인해주세요. 예) https://chzzk.naver.com/(여기가 채널 ID)")]
        public string ChzzkChannelId { get; set; } = "YOUR_CHANNEL_ID";

        [Description("이벤트를 설정합니다.")]
        public EventConfig EventConfig { get; set; } = new();
    }

    public class EventConfig
    {
        [Description("이벤트 후원량을 설정합니다.")]
        public Dictionary<int, EventType> EventTypes { get; set; } = new Dictionary<int, EventType>()
        {
            { 5000, EventType.Bomb },
            { 10000, EventType.Ensnared },
            { 15000, EventType.DeleteRandomItem },
            { 20000, EventType.DropAllItem },
            { 30000, EventType.Blackout },
            { 40000, EventType.Lockdown },
            { 50000, EventType.BringRandomScp },
            { 70000, EventType.BringAllPlayers },
            { 80000, EventType.StartWarhead },
            { 100000, EventType.DetonateWarhead },
        };

        [Description("후원 시 자막을 설정합니다.")]
        public Dictionary<EventType, string> EventSubtitles { get; set; } = new Dictionary<EventType, string>()
        {
            { EventType.Bomb, "폭탄을 드시랍니다!" },
            { EventType.BringRandomScp, "랜덤 SCP를 데려오시랍니다!" },
            { EventType.DeleteRandomItem, "랜덤 아이템을 삭제시키시랍니다!" },
            { EventType.DropAllItem, "모든 아이템을 버리시랍니다!" },
            { EventType.BringAllPlayers, "모든 플레이어를 데려오시랍니다!" },
            { EventType.StartWarhead, "핵탄두를 시작시키시랍니다!" },
            { EventType.DetonateWarhead, "핵탄두를 폭파시키시랍니다!" },
            { EventType.Lockdown, "봉쇄시키시랍니다!" },
            { EventType.Blackout, "정전시키시랍니다!" },
            { EventType.Ensnared, "움직임을 멈추시랍니다!" }
        };

        [Description("후원 자막의 포맷을 설정합니다.")]
        public string EventSubtitleFormat { get; set; } = "<b><size=30>{0}</b>님이 {1}원으로 <b>{2}</b>\n{3}</size>";

        [Description("폭탄 관련 이벤트를 설정합니다.")]
        public BombConfig Bomb { get; set; } = new();

        [Description("포박 상태 이상 관련 이벤트를 설정합니다.")]
        public EnsnaredConfig Ensnared { get; set; } = new();

        [Description("정전 관련 이벤트를 설정합니다.")]
        public BlackoutConfig Blackout { get; set; } = new();

        [Description("봉쇄 관련 이벤트를 설정합니다.")]
        public LockdownConfig Lockdown { get; set; } = new();
    }

    public enum EventType
    {
        Bomb,
        BringRandomScp,
        DeleteRandomItem,
        DropAllItem,
        BringAllPlayers,
        StartWarhead,
        DetonateWarhead,
        Lockdown,
        Blackout,
        Ensnared,
    }

    public class BombConfig
    {
        [Description("폭탄 점화 시간을 설정합니다.")]
        public float FuseTime { get; set; } = 3f;
    }

    public class EnsnaredConfig
    {
        [Description("포박 상태 이상의 시간을 설정합니다.")]
        public float Duration { get; set; } = 3f;
    }

    public class BlackoutConfig
    {
        [Description("정전 시간을 설정합니다.")]
        public float Duration { get; set; } = 3f;
    }

    public class LockdownConfig
    {
        [Description("봉쇄 시간을 설정합니다.")]
        public float Duration { get; set; } = 5f;
    }
}