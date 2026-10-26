using StardewModdingAPI;

namespace StardewFaster
{
    public class ModConfig
    {
        public TreeFertilizerConfig TreeFertilizer { get; set; } = new();
        public EatingConfig Eating { get; set; } = new();
        public NoAnimalCollisionConfig NoAnimalCollision { get; set; } = new();
        public MemoryOverlayConfig MemoryOverlay { get; set; } = new();
        public RobinUpgradeConfig RobinUpgrade { get; set; } = new();
        public ResetDayTimeConfig ResetDayTime { get; set; } = new();
    }

    public class TreeFertilizerConfig
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Growth stages a fertilized wild tree gains per night (vanilla Tree Fertilizer = 1).</summary>
        public int WildTreeStagesPerNight { get; set; } = 2;

        /// <summary>Extra days of growth a fertilized fruit tree gains per night, on top of the normal 1.</summary>
        public int FruitTreeExtraDaysPerNight { get; set; } = 2;
    }

    public class EatingConfig
    {
        public bool Enabled { get; set; } = true;

        /// <summary>How many times faster the animation plays (1 = vanilla).</summary>
        public float SpeedMultiplier { get; set; } = 2f;
    }
    
    public class NoAnimalCollisionConfig
    {
        public bool Enabled { get; set; } = true;
 
        /// <summary>Walk through farm animals (chickens, cows, etc.).</summary>
        public bool FarmAnimals { get; set; } = true;
 
        /// <summary>Walk through pets (cat/dog/turtle).</summary>
        public bool Pets { get; set; } = true;
    }
    
    public class MemoryOverlayConfig
    {
        public bool Enabled { get; set; } = true;
 
        /// <summary>Whether the overlay is visible when the game starts.</summary>
        public bool ShowOnStart { get; set; } = false;
 
        /// <summary>Key that shows/hides the overlay.</summary>
        public SButton ToggleKey { get; set; } = SButton.F9;
 
        public int OffsetX { get; set; } = 16;
        public int OffsetY { get; set; } = 16;
 
        /// <summary>Write a SMAPI log line whenever a frame takes longer than the threshold, saying whether a GC happened in it.</summary>
        public bool LogHitches { get; set; } = true;
 
        public float HitchThresholdMs { get; set; } = 100f;
    }
    
    public class RobinUpgradeConfig
    {
        public bool Enabled { get; set; } = false;
 
        /// <summary>Cost of the upgrade, in gold.</summary>
        public int Price { get; set; } = 200000;
 
        /// <summary>First in-game year the upgrade is offered.</summary>
        public int AvailableFromYear { get; set; } = 3;
 
        /// <summary>Building speed bonus in percent (25 = jobs take 1/1.25 of the time).</summary>
        public int SpeedBonusPercent { get; set; } = 25;
    }

    public class ResetDayTimeConfig
    {
        public bool Enabled { get; set; } = true;

        /// <summary>Key that resets the clock to 6:00 AM.</summary>
        public SButton ResetKey { get; set; } = SButton.F12;
    }

}