using StardewModdingAPI;
using StardewTweaks.Features;

namespace StardewTweaks
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            ModConfig config = helper.ReadConfig<ModConfig>();

            if (config.TreeFertilizer.Enabled)
                new TreeFertilizerFeature(this, config.TreeFertilizer).Register();

            if (config.Eating.Enabled)
                new EatingFeature(this, config.Eating).Register();
            
            if(config.NoAnimalCollision.Enabled)
                new NoAnimalCollisionFeature(this, config.NoAnimalCollision).Register();
            
            if(config.MemoryOverlay.Enabled)
                new MemoryOverlayFeature(this, config.MemoryOverlay).Register();
        }
    }
}