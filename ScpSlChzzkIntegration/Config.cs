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

        [Description("치지직 채널의 ID를 입력합니다.")]
        public string ChzzkChannelId { get; set; } = "YOUR_CHANNEL_ID";
    }
}