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
        internal const string Known = "Gwyn, Artorias, ArtoriasPhantom, SoulOfCinder, DarkKnight, DarkBloodKnight, OolacileCultist";

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

            // ArtoriasPhantom wears Artorias's exact loadout (shared ArtoriasSwordsman base); in game only its
            // spectral tint differs, which this renderer does not draw.
            if (puppet.Equals("Artorias", StringComparison.OrdinalIgnoreCase)
                || puppet.Equals("ArtoriasPhantom", StringComparison.OrdinalIgnoreCase))
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

            if (puppet.Equals("SoulOfCinder", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "SoulOfCinder",
                    BodySheet = P("Items", "Armors", "FirelinkArmor_Body.png"),
                    LegsSheet = P("Items", "Armors", "FirelinkLeggings_Legs.png"),
                    HeadSheet = P("Items", "Armors", "FirelinkHelm_Head.png"),
                    WeaponSprite = P("Items", "Weapons", "Melee", "Broadswords", "SeveringDusk.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1.15f,
                };
            }

            if (puppet.Equals("DarkKnight", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "DarkKnight",
                    BodySheet = P("Items", "Armors", "Melee", "DarkKnightArmor_Body.png"),
                    LegsSheet = P("Items", "Armors", "Melee", "DarkKnightGreaves_Legs.png"),
                    HeadSheet = P("Items", "Armors", "Melee", "DarkKnightHelmet_Head.png"),
                    // SwingPreview cannot load vanilla XNBs headlessly. Rune Blade has the same
                    // compact broadsword footprint and is used only as the offline pose proxy;
                    // the game draws the real vanilla Night's Edge selected by DarkKnight.cs.
                    WeaponSprite = P("Items", "Weapons", "Melee", "Broadswords", "RuneBlade.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            if (puppet.Equals("DarkBloodKnight", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "DarkBloodKnight",
                    BodySheet = P("Items", "Armors", "Melee", "DarkKnightArmor_Body.png"),
                    LegsSheet = P("Items", "Armors", "Melee", "DarkKnightGreaves_Legs.png"),
                    HeadSheet = P("Items", "Armors", "Melee", "DarkKnightHelmet_Head.png"),
                    WeaponSprite = P("Projectiles", "Enemy", "Weapons", "BloodSword.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            if (puppet.Equals("OolacileCultist", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "OolacileCultist",
                    // The game draws vanilla Brain of Cthulhu Mask + Solar Cultist Robe, which this tool cannot
                    // load headlessly. Kahlrun's red cloth set is an offline body proxy only; the claw is real.
                    BodySheet = P("Items", "Armors", "Magic", "RedClothTunic_Body.png"),
                    LegsSheet = P("Items", "Armors", "Magic", "RedClothPants_Legs.png"),
                    HeadSheet = P("Items", "Armors", "Magic", "RedClothHat_Head.png"),
                    WeaponSprite = P("Projectiles", "Enemy", "Weapons", "BeastClaw.png"),
                    OffHandWeaponSprite = P("Projectiles", "Enemy", "Weapons", "BeastClaw.png"),
                    OffHandWeaponScale = 0.5f,     // OolacileCultist.OffHandClawDrawScale
                    OffHandCarryRotation = 0.485f, // OolacileCultist.ClawCarryRotation (-0.30 + PiOver4)
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            return null;
        }
    }
}
