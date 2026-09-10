using System;
using System.IO;

namespace SwingPreview
{
    /// <summary>
    /// Sprite-sheet paths per puppet for the offline body render.
    ///
    /// Kept as a small explicit table rather than resolved through tModLoader's asset system, which
    /// needs a running game. Adding a puppet is four paths; the framing itself is fixed by vanilla
    /// (body sheets are 9x4 composite cells of 40x56, legs and head are 40x1120 legacy strips).
    /// </summary>
    internal static class PuppetArtLibrary
    {
        internal const string Known = "Gwyn, Artorias";

        internal static PuppetArt Resolve(string puppet, string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(puppet)) { return null; }

            string P(params string[] parts) => Path.GetFullPath(Path.Combine(repoRoot, Path.Combine(parts)));

            if (puppet.Equals("Gwyn", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "Gwyn",
                    BodySheet = P("Items", "Armors", "LordGwynArmor_Body.png"),
                    LegsSheet = P("Items", "Armors", "LordGwynLeggings_Legs.png"),
                    HeadSheet = P("Items", "Armors", "LordGwynHelm_Head.png"),
                    // EnemySwordOfGwyn overrides Texture to the player sword's sprite.
                    WeaponSprite = P("Items", "Weapons", "Melee", "Broadswords", "SwordOfGwyn.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1.1f,
                };
            }

            if (puppet.Equals("Artorias", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "Artorias",
                    BodySheet = P("Items", "Armors", "Melee", "ArtoriasArmor_Body.png"),
                    LegsSheet = P("Items", "Armors", "Melee", "ArtoriasGreaves_Legs.png"),
                    HeadSheet = P("Items", "Armors", "Melee", "ArtoriasHelmet_Head.png"),
                    WeaponSprite = P("Items", "Weapons", "Melee", "Broadswords", "ArtoriasGreatsword.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1.1f,
                };
            }

            return null;
        }
    }
}
