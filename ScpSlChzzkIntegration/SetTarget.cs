using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace ScpSlChzzkIntegration;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class SetTarget : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
    {
        if (arguments.Count < 1)
        {
            Plugin.Instance.EventHandler.Target = null;
            response = "대상을 초기화했습니다.";
            return false;
        }

        var text = string.Join(" ", arguments);
        var player = Player.Get(text);

        if (player == null)
        {
            Plugin.Instance.EventHandler.Target = null;
            response = "대상을 초기화했습니다.";
            return false;
        }

        Plugin.Instance.EventHandler.Target = player;
        response = $"대상을 {player.Nickname}으로 설정했습니다.";
        return true;
    }

    public string Command { get; } = "settarget";
    public string[] Aliases { get; } = ["st"];
    public string Description { get; } = "후원 이벤트의 대상을 설정합니다.";
    public bool SanitizeResponse { get; } = false;
}