using System;
using Exiled.API.Features;
using ScpSlChzzkIntegration.API;
using Player = Exiled.Events.Handlers.Player;

namespace ScpSlChzzkIntegration
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public override string Name { get; } = "ScpSlChzzkIntegration";
        public override string Author { get; } = "Cocoa";
        public override string Prefix { get; } = "ScpSlChzzkIntegration";
        public override Version Version { get; } = new(1, 0, 0);

        public EventHandler EventHandler { get; private set; }
        public Chzzk Chzzk { get; private set; }

        public override void OnEnabled()
        {
            base.OnEnabled();

            Instance = this;

            Chzzk = new Chzzk();
            Chzzk.ConnectChzzk(Config.ChzzkChannelId);

            EventHandler = new EventHandler(this);
            Chzzk.OnMessage += EventHandler.OnMessage;
            Chzzk.OnDonation += EventHandler.OnDonation;
        }

        public override void OnDisabled()
        {
            base.OnDisabled();

            Chzzk.RemoveAllOnDonationListener();
            Chzzk.RemoveAllOnMessageListener();
            Chzzk.StopListening();
            Chzzk = null;

            EventHandler = null;

            Instance = null;
        }
    }
}