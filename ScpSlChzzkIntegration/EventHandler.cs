using Exiled.API.Features;
using ScpSlChzzkIntegration.API;

#pragma warning disable CS0169 // Field is never used

namespace ScpSlChzzkIntegration;

public class EventHandler(Plugin plugin)
{
    public Plugin Plugin { get; } = plugin;

    public void OnMessage(Profile profile, string message)
    {
        Log.Info($"[치지직] {profile.nickname}: {message}");
    }

    public void OnDonation(Profile profile, string message, DonationExtras extras)
    {
        if (profile == null)
        {
            Log.Info($"[치지직] 익명의 후원자님이 {extras.payAmount}원을 후원했습니다: {message}");
            return;
        }

        Log.Info($"[치지직] {profile.nickname}님이 {extras.payAmount}원을 후원했습니다: {message}");
    }
}