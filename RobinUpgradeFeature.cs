using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace StardewFaster.Features
{
    /// <summary>
    /// Adds a one-time purchase to Robin's existing counter menu (from year 4) that permanently
    /// speeds up construction and upgrades of farm buildings.
    /// </summary>
    public class RobinUpgradeFeature
    {
        private const string OfferKey = "StardewFaster_RobinTools";
        private const string PurchaseMessage = "PurchaseRobinUpgrade";
        private const string PurchasedKey = "StardewFaster/RobinToolsUpgrade";
        private const string CreditKey = "StardewFaster/RobinBuildCredit";
        private const string ProcessedKey = "StardewFaster/BuildSpeedApplied";

        private readonly Mod Mod;
        private readonly RobinUpgradeConfig Config;

        /// <summary>Set right after a farmhand buys the upgrade, until the host's saved flag syncs back to them.</summary>
        private bool UnlockedLocally;

        /// <summary>An action to run on the next tick (per player), used so dialogues don't overlap.</summary>
        private readonly PerScreen<Action?> Pending = new();

        public RobinUpgradeFeature(Mod mod, RobinUpgradeConfig config)
        {
            Mod = mod;
            Config = config;
        }

        public void Register()
        {
            IModEvents events = Mod.Helper.Events;
            events.Display.MenuChanged += OnMenuChanged;
            events.GameLoop.UpdateTicked += OnUpdateTicked;
            events.GameLoop.DayEnding += OnDayEnding;
            events.GameLoop.DayStarted += OnDayStarted;
            events.GameLoop.ReturnedToTitle += (_, _) => UnlockedLocally = false;
            events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private bool IsPurchased()
        {
            return UnlockedLocally || Game1.MasterPlayer.modData.ContainsKey(PurchasedKey);
        }

        /// <summary>Record the purchase in the host's save (the flag then syncs to every player).</summary>
        private static void Unlock()
        {
            Game1.MasterPlayer.modData[PurchasedKey] = "1";
        }

        /// <summary>Host: a farmhand bought the upgrade.</summary>
        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer || e.FromModID != Mod.ModManifest.UniqueID || e.Type != PurchaseMessage)
                return;

            Unlock();
        }

        // ------------------------------------------------------------------
        // Robin's counter menu
        // ------------------------------------------------------------------

        /// <summary>When Robin's menu opens, add our option next to the existing ones.</summary>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is not DialogueBox box || IsPurchased() || Game1.year < Config.AvailableFromYear)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null || location.Name != "ScienceHouse" || box.responses == null)
                return;

            // Robin's menu is the one with her usual "Shop" / "Construct Farm Buildings" choices.
            bool isRobinMenu = box.responses.Any(r => r.responseKey == "Construct" || r.responseKey == "Shop");
            if (!isRobinMenu || box.responses.Any(r => r.responseKey == OfferKey))
                return;

            // Build the full list of choices, with ours just above "Leave" (or last if there is no such choice).
            List<Response> choices = box.responses.ToList();
            Response offer = new Response(OfferKey, $"Upgrade Robin's tools ({Config.Price:N0}g)");
            int leaveIndex = choices.FindIndex(r => r.responseKey == "Leave");
            if (leaveIndex >= 0)
                choices.Insert(leaveIndex, offer);
            else
                choices.Add(offer);

            // Re-open the same question with the extra choice. A fresh dialogue box sizes itself to fit
            // every option, which is more reliable than resizing the existing one.
            string question = box.dialogues != null && box.dialogues.Count > 0 ? box.dialogues[0] : "";
            string questionKey = location.lastQuestionKey;
            location.createQuestionDialogue(question, choices.ToArray(), questionKey);

            // Handle our choice ourselves and pass every other choice back to the game unchanged.
            location.afterQuestion = (who, key) =>
            {
                location.afterQuestion = null;

                if (key == OfferKey)
                    Pending.Value = () => TryPurchase(who);
                else
                    location.answerDialogue(new Response(key, ""));
            };
        }

        private void TryPurchase(Farmer who)
        {
            if (IsPurchased())
            {
                Game1.drawObjectDialogue("Robin's tools have already been upgraded.");
                return;
            }

            if (who.Money < Config.Price)
            {
                Game1.drawObjectDialogue($"You need {Config.Price:N0}g for this upgrade.");
                return;
            }

            who.Money -= Config.Price;

            if (Context.IsMainPlayer)
            {
                Unlock();
            }
            else
            {
                // The host stores the flag and applies the faster building, so tell the host.
                UnlockedLocally = true;
                Mod.Helper.Multiplayer.SendMessage(
                    "1",
                    PurchaseMessage,
                    new[] { Mod.ModManifest.UniqueID },
                    new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }

            Game1.playSound("purchaseClick");
            Game1.drawObjectDialogue($"Robin's tools upgraded! Buildings and upgrades are now {Config.SpeedBonusPercent}% faster to build.");
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            Action? action = Pending.Value;
            if (action == null)
                return;

            Pending.Value = null;
            action();
        }

        // ------------------------------------------------------------------
        // Faster building
        // ------------------------------------------------------------------

        /// <summary>Before the nightly update, shorten any construction/upgrade that hasn't been processed yet.</summary>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (!Context.IsMainPlayer || !IsPurchased())
                return;

            Utility.ForEachLocation(location =>
            {
                foreach (Building building in location.buildings)
                    ApplySpeedBonus(building);

                return true;
            });
        }

        /// <summary>Forget finished jobs so the next construction or upgrade is processed again.</summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;

            Utility.ForEachLocation(location =>
            {
                foreach (Building building in location.buildings)
                {
                    if (building.daysOfConstructionLeft.Value <= 0 && building.daysUntilUpgrade.Value <= 0)
                        building.modData.Remove(ProcessedKey);
                }

                return true;
            });
        }

        private void ApplySpeedBonus(Building building)
        {
            bool constructing = building.daysOfConstructionLeft.Value > 0;
            bool upgrading = building.daysUntilUpgrade.Value > 0;
            if (!constructing && !upgrading)
                return;

            if (building.modData.ContainsKey(ProcessedKey))
                return;

            var field = constructing ? building.daysOfConstructionLeft : building.daysUntilUpgrade;
            int days = field.Value;

            // Speed +25% means the job takes 1 / 1.25 of the time, so it saves 20% of its days.
            // Days are whole numbers, so unused fractions are banked and carried to the next job.
            double saved = days * (1.0 - 1.0 / (1.0 + Config.SpeedBonusPercent / 100.0));
            double credit = ReadCredit() + saved;

            // Keep at least 1 day so the game's own nightly update still finishes the job properly.
            int reduction = Math.Min((int)Math.Floor(credit), days - 1);
            if (reduction > 0)
            {
                field.Value = days - reduction;
                credit -= reduction;
            }

            WriteCredit(credit);
            building.modData[ProcessedKey] = "1";
        }

        private static double ReadCredit()
        {
            if (Game1.MasterPlayer.modData.TryGetValue(CreditKey, out string? raw)
                && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return value;

            return 0;
        }

        private static void WriteCredit(double value)
        {
            Game1.MasterPlayer.modData[CreditKey] = value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}