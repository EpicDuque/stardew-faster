using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace StardewTweaks.Features
{
    /// <summary>Lets the player walk through farm animals and pets.</summary>
    public class NoAnimalCollisionFeature
    {
        private readonly Mod Mod;
        private readonly NoAnimalCollisionConfig Config;

        public NoAnimalCollisionFeature(Mod mod, NoAnimalCollisionConfig config)
        {
            Mod = mod;
            Config = config;
        }

        public void Register()
        {
            IModEvents events = Mod.Helper.Events;
            events.GameLoop.UpdateTicked += OnUpdateTicked;
            events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) Apply(e.NewLocation); };
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // Re-apply twice a second so newly born/purchased animals are covered too.
            if (!Context.IsWorldReady || !e.IsMultipleOf(30))
                return;

            Apply(Game1.currentLocation);
        }

        private void Apply(GameLocation? location)
        {
            if (location == null)
                return;

            // "farmerPassesThrough" is the flag the game uses for things like Junimos:
            // the player's movement ignores the character's collision box.
            if (Config.FarmAnimals)
            {
                foreach (FarmAnimal animal in location.animals.Values)
                    animal.farmerPassesThrough = true;
            }

            if (Config.Pets)
            {
                foreach (Pet pet in location.characters.OfType<Pet>())
                    pet.farmerPassesThrough = true;
            }
        }
    }
}