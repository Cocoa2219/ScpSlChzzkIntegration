using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using PlayerRoles;
using ScpSlChzzkIntegration.API;
using UnityEngine;

#pragma warning disable CS0169 // Field is never used

namespace ScpSlChzzkIntegration;

public class EventHandler(Plugin plugin)
{
    public Plugin Plugin { get; } = plugin;

    public Player Target { get; set; }

    public void OnMessage(Profile profile, string message)
    {
        Log.Debug(profile == null
            ? $"[치지직] 익명의 시청자: {message}"
            : $"[치지직] {profile.nickname}: {message}");
    }

    public void OnDonation(Profile profile, string message, DonationExtras extras)
    {
        Log.Debug(profile == null
            ? $"[치지직] 익명의 후원자 ({extras.payAmount}원): {message}"
            : $"[치지직] {profile.nickname} ({extras.payAmount}원): {message}");

        if (!Round.IsStarted)
            return;

        DonationEvent(profile, message, extras);
    }

    public EventType? GetNearestEventType(int value, Dictionary<int, EventType> eventTypes)
    {
        eventTypes = eventTypes.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        var closestKey = -1;
        foreach (var key in eventTypes.Keys.Where(key => key <= value && key > closestKey))
        {
            closestKey = key;
        }

        if (closestKey == -1)
        {
            return null;
        }

        return eventTypes[closestKey];
    }


    private void DonationEvent(Profile profile, string msg, DonationExtras extras)
    {
        if (Target == null)
            return;

        var amount = extras.payAmount;
        var nickname = profile?.nickname ?? "익명의 후원자";

        var eventType = GetNearestEventType(amount, Plugin.Config.EventConfig.EventTypes);

        if (eventType == null)
            return;

        if (Plugin.Config.EventConfig.EventSubtitles.TryGetValue(eventType.Value, out var subtitle))
        {
            var format = Plugin.Config.EventConfig.EventSubtitleFormat;

            var message = string.Format(format, nickname, amount, subtitle, msg);

            Map.Broadcast(5, message);
        }

        switch (eventType.Value)
        {
            case EventType.Bomb:
                Bomb();
                break;
            case EventType.BringRandomScp:
                BringRandomScp();
                break;
            case EventType.DeleteRandomItem:
                DeleteRandomItem();
                break;
            case EventType.DropAllItem:
                DropAllItem();
                break;
            case EventType.BringAllPlayers:
                BringAllPlayers();
                break;
            case EventType.StartWarhead:
                StartWarhead();
                break;
            case EventType.DetonateWarhead:
                DetonateWarhead();
                break;
            case EventType.Lockdown:
                Lockdown();
                break;
            case EventType.Blackout:
                Blackout();
                break;
            case EventType.Ensnared:
                Ensnared();
                break;
        }

    }

    private void Bomb()
    {
        var pos = Target.Position + new Vector3(0, 0.5f, 0);
        var grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);

        grenade.FuseTime = Plugin.Config.EventConfig.Bomb.FuseTime;
        grenade.SpawnActive(pos, Target);
    }

    private void BringRandomScp()
    {
        var scp = Player.Get(Team.SCPs).GetRandomValue();
        var pos = Target.Position + new Vector3(0, 0.5f, 0);

        scp.Position = pos;
    }

    private void DeleteRandomItem()
    {
        var item = Target.Items.GetRandomValue();

        Target.RemoveItem(item);
    }

    private void DropAllItem()
    {
        Target.DropItems();
    }

    private void BringAllPlayers()
    {
        foreach (var player in Player.List)
        {
            if (player == Target)
                continue;

            player.Position = Target.Position + new Vector3(0, 0.5f, 0);
        }
    }

    private void StartWarhead()
    {
        Warhead.Start();
    }

    private void DetonateWarhead()
    {
        Warhead.Detonate();
    }

    private void Lockdown()
    {
        var room = Target.CurrentRoom;

        room.LockDown(Plugin.Config.EventConfig.Lockdown.Duration, DoorLockType.AdminCommand);
    }

    private void Blackout()
    {
        var room = Target.CurrentRoom;

        room.TurnOffLights(Plugin.Config.EventConfig.Blackout.Duration);
    }

    private void Ensnared()
    {
        Target.EnableEffect(EffectType.Ensnared, Plugin.Config.EventConfig.Ensnared.Duration);
    }
}