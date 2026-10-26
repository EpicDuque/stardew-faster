using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.TerrainFeatures;

namespace StardewFaster.Features
{
    /// <summary>Adds Hyper Tree Fertilizer: a craftable item that makes wild and fruit trees grow much faster.</summary>
    public class TreeFertilizerFeature
    {
        private const string ItemName = "HyperTreeFertilizer";
        private const int MatureWildStage = 5;

        private readonly Mod Mod;
        private readonly TreeFertilizerConfig Config;
        private readonly string ItemId;
        private readonly string TagKey;

        public TreeFertilizerFeature(Mod mod, TreeFertilizerConfig config)
        {
            this.Mod = mod;
            this.Config = config;
            this.ItemId = $"{mod.ModManifest.UniqueID}_{ItemName}";
            this.TagKey = $"{mod.ModManifest.UniqueID}/fertilized";
        }

        public void Register()
        {
            IModEvents events = this.Mod.Helper.Events;
            events.Content.AssetRequested += this.OnAssetRequested;
            events.Input.ButtonPressed += this.OnButtonPressed;
            events.GameLoop.DayStarted += this.OnDayStarted;
        }

        /// <summary>Add the item and its crafting recipe.</summary>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[this.ItemId] = new ObjectData
                    {
                        Name = ItemName,
                        DisplayName = "Hyper Tree Fertilizer",
                        Description = "Apply to a tree to make it grow far faster than normal. Works on wild and fruit trees.",
                        Type = "Basic",
                        Category = 0,
                        Price = 150,
                        Texture = this.Mod.Helper.ModContent.GetInternalAssetName("assets/icon.png").BaseName,
                        SpriteIndex = 0,
                        Edibility = -300
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    // 5 Tree Fertilizer (805) + 5 Hardwood (709) -> 5 Hyper Tree Fertilizer, unlocked at Foraging 8
                    asset.AsDictionary<string, string>().Data[ItemName] =
                        $"805 5 709 5/Home/{this.ItemId} 5/false/Foraging 8/Hyper Tree Fertilizer";
                });
            }
        }

        /// <summary>Apply the fertilizer when using it on a tree.</summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree || !e.Button.IsUseToolButton())
                return;

            Item held = Game1.player.CurrentItem;
            if (held?.ItemId != this.ItemId)
                return;

            Vector2 tile = e.Cursor.GrabTile;
            if (Vector2.Distance(tile, Game1.player.Tile) > 3f)
                return;

            if (!Game1.currentLocation.terrainFeatures.TryGetValue(tile, out TerrainFeature feature))
                return;

            bool canApply = feature switch
            {
                Tree tree => tree.growthStage.Value < MatureWildStage && !tree.stump.Value,
                FruitTree ft => ft.daysUntilMature.Value > 0,
                _ => false
            };
            if (!canApply || feature.modData.ContainsKey(this.TagKey))
                return;

            feature.modData[this.TagKey] = "1";
            Game1.player.reduceActiveItemByOne();
            Game1.currentLocation.playSound("dirtyHit");
            this.Mod.Helper.Input.Suppress(e.Button);
        }

        /// <summary>Grow tagged trees overnight.</summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Utility.ForEachLocation(location =>
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    TerrainFeature feature = pair.Value;
                    if (!feature.modData.ContainsKey(this.TagKey))
                        continue;

                    if (feature is Tree tree)
                        this.GrowWildTree(location, pair.Key, tree);
                    else if (feature is FruitTree ft)
                        this.GrowFruitTree(ft);
                }
                return true;
            });
        }

        private void GrowWildTree(GameLocation location, Vector2 tile, Tree tree)
        {
            for (int i = 0; i < this.Config.WildTreeStagesPerNight; i++)
            {
                int next = tree.growthStage.Value + 1;
                if (next > MatureWildStage)
                    break;
                if (next == MatureWildStage && this.HasMatureNeighbor(location, tile))
                    break; // vanilla rule: needs space to fully grow

                tree.growthStage.Value = next;
            }

            if (tree.growthStage.Value >= MatureWildStage)
                tree.modData.Remove(this.TagKey);
        }

        private void GrowFruitTree(FruitTree ft)
        {
            if (ft.daysUntilMature.Value > 0)
            {
                ft.daysUntilMature.Value = System.Math.Max(0, ft.daysUntilMature.Value - this.Config.FruitTreeExtraDaysPerNight);
                int d = ft.daysUntilMature.Value;
                ft.growthStage.Value = d > 28 ? 0 : d > 14 ? 1 : d > 7 ? 2 : d > 0 ? 3 : 4;
            }

            if (ft.daysUntilMature.Value <= 0)
                ft.modData.Remove(this.TagKey);
        }

        private bool HasMatureNeighbor(GameLocation location, Vector2 tile)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    if (location.terrainFeatures.TryGetValue(tile + new Vector2(dx, dy), out TerrainFeature other)
                        && other is Tree t && t.growthStage.Value >= MatureWildStage)
                        return true;
                }
            }
            return false;
        }
    }
}