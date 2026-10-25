using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewTweaks.Features
{
    /// <summary>Shortens the eating and drinking animations.</summary>
    public class EatingFeature
    {
        private readonly Mod Mod;
        private readonly EatingConfig Config;

        /// <summary>The animation list we already sped up (per player), so each animation is only scaled once.</summary>
        private readonly PerScreen<object?> ScaledAnimation = new();

        public EatingFeature(Mod mod, EatingConfig config)
        {
            Mod = mod;
            Config = config;
        }

        public void Register()
        {
            Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;

            if (!player.isEating)
            {
                ScaledAnimation.Value = null;
                return;
            }

            var sprite = player.FarmerSprite;
            var frames = sprite.CurrentAnimation;
            if (frames == null || frames.Count == 0)
                return; // animation not set up yet; try again next tick

            // Already sped up this exact animation for this player? Never scale it twice.
            if (ReferenceEquals(ScaledAnimation.Value, frames))
                return;

            float speed = Math.Max(1f, Config.SpeedMultiplier);

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                frame.milliseconds = Math.Max(1, (int)(frame.milliseconds / speed));
                frames[i] = frame;
            }

            sprite.interval = Math.Max(1f, sprite.interval / speed);
            sprite.timer = Math.Min(sprite.timer, sprite.interval);

            ScaledAnimation.Value = frames;
        }
    }
}