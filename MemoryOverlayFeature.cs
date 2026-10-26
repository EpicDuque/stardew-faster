using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewFaster.Features
{
    /// <summary>
    /// Shows memory use, garbage-collection activity and frame time on screen, and logs
    /// long frames together with whether a GC happened during them (to confirm or rule out GC spikes).
    /// </summary>
    public class MemoryOverlayFeature
    {
        private readonly Mod Mod;
        private readonly MemoryOverlayConfig Config;
        private readonly Process Proc = Process.GetCurrentProcess();
        private readonly Stopwatch Clock = Stopwatch.StartNew();

        private bool Visible;

        // frame timing
        private double LastTickMs = -1;
        private double FrameMs;
        private double WindowMaxMs;
        private double WindowStartMs;
        private double ShownMaxMs;
        private double LastHitchAtMs = -100000;

        // GC tracking
        private long LastGcIndex;
        private double LastGcAtMs = -100000;
        private string LastGcText = "none yet";

        // process memory (refreshed twice a second)
        private double WorkingSetMb;
        private double NextProcRefreshMs;

        public MemoryOverlayFeature(Mod mod, MemoryOverlayConfig config)
        {
            this.Mod = mod;
            this.Config = config;
            this.Visible = config.ShowOnStart;
        }

        public void Register()
        {
            IModEvents events = this.Mod.Helper.Events;
            events.GameLoop.UpdateTicking += this.OnUpdateTicking;
            events.Display.RenderedHud += this.OnRenderedHud;
            events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == this.Config.ToggleKey)
                this.Visible = !this.Visible;
        }

        /// <summary>Measure the time between frames and detect garbage collections.</summary>
        private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            // In split-screen the update runs once per player each frame; only count the first screen.
            if (Context.ScreenId != 0)
                return;

            double now = this.Clock.Elapsed.TotalMilliseconds;

            // Don't measure while loading or while the window is in the background (alt-tab causes fake hitches).
            if (!Context.IsWorldReady || !Game1.game1.IsActive)
            {
                this.LastTickMs = -1;
                return;
            }

            GCMemoryInfo info = GC.GetGCMemoryInfo();

            if (this.LastTickMs < 0)
            {
                this.LastTickMs = now;
                this.LastGcIndex = info.Index;
                return;
            }

            this.FrameMs = now - this.LastTickMs;
            this.LastTickMs = now;

            // Did a garbage collection happen since the last frame?
            bool gcThisFrame = info.Index != this.LastGcIndex;
            if (gcThisFrame)
            {
                this.LastGcIndex = info.Index;
                double pauseMs = info.PauseDurations.Length > 0 ? info.PauseDurations[0].TotalMilliseconds : 0;
                this.LastGcAtMs = now;
                this.LastGcText = $"gen{info.Generation}, {pauseMs:F1} ms pause";
            }

            // Highest frame time in the last second.
            if (now - this.WindowStartMs >= 1000)
            {
                this.ShownMaxMs = this.WindowMaxMs;
                this.WindowMaxMs = 0;
                this.WindowStartMs = now;
            }
            this.WindowMaxMs = Math.Max(this.WindowMaxMs, this.FrameMs);

            // Long frame: log it and say whether a GC was involved.
            if (this.FrameMs >= this.Config.HitchThresholdMs)
            {
                this.LastHitchAtMs = now;
                if (this.Config.LogHitches)
                {
                    double managedMb = GC.GetTotalMemory(false) / 1048576.0;
                    string gcInfo = gcThisFrame ? $"YES ({this.LastGcText})" : "no";
                    this.Mod.Monitor.Log($"Hitch: frame took {this.FrameMs:F0} ms. GC during this frame: {gcInfo}. Managed heap: {managedMb:F0} MB.", LogLevel.Warn);
                }
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!this.Visible || !Context.IsWorldReady || Context.ScreenId != 0)
                return;

            double now = this.Clock.Elapsed.TotalMilliseconds;

            if (now >= this.NextProcRefreshMs)
            {
                this.Proc.Refresh();
                this.WorkingSetMb = this.Proc.WorkingSet64 / 1048576.0;
                this.NextProcRefreshMs = now + 500;
            }

            double managedMb = GC.GetTotalMemory(false) / 1048576.0;
            double sinceGcSec = (now - this.LastGcAtMs) / 1000.0;
            string lastGc = this.LastGcAtMs < 0 ? "none yet" : $"{this.LastGcText}, {sinceGcSec:F0}s ago";

            string text =
                $"Process memory: {this.WorkingSetMb:F0} MB\n" +
                $"Managed heap: {managedMb:F0} MB\n" +
                $"GC count: g0 {GC.CollectionCount(0)} | g1 {GC.CollectionCount(1)} | g2 {GC.CollectionCount(2)}\n" +
                $"Last GC: {lastGc}\n" +
                $"Frame: {this.FrameMs:F1} ms (max 1s: {this.ShownMaxMs:F0} ms)";

            // Red = recent long frame, yellow = GC just happened.
            Color color = now - this.LastHitchAtMs < 2000 ? Color.Red
                        : now - this.LastGcAtMs < 1000 ? Color.Yellow
                        : Color.White;

            SpriteBatch b = e.SpriteBatch;
            Vector2 size = Game1.smallFont.MeasureString(text);
            Vector2 pos = new(this.Config.OffsetX, this.Config.OffsetY);

            b.Draw(Game1.fadeToBlackRect,
                new Rectangle((int)pos.X - 8, (int)pos.Y - 6, (int)size.X + 16, (int)size.Y + 12),
                Color.Black * 0.65f);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, color);
        }
    }
}