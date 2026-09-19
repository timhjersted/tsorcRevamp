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
        internal const string Known = "Gwyn, Artorias, ArtoriasPhantom, SoulOfCinder, DarkKnight, DarkBloodKnight, OolacileCultist, AbysmalOolacileSorcerer, BlackNinja, DreadWraith";

        internal static PuppetArt Resolve(string puppet, string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(puppet)) { return null; }

            string P(params string[] parts) => Path.GetFullPath(Path.Combine(repoRoot, Path.Combine(parts)));

            if (puppet.Equals("Gwyn", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "Gwyn",
                    BodySheet = P("Content", "Items", "Armor", "LordGwynArmor_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "LordGwynLeggings_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "LordGwynHelm_Head.png"),
                    // EnemySwordOfGwyn overrides Texture to the player sword's sprite.
                    WeaponSprite = P("Content", "Items", "Weapons", "Melee", "Broadswords", "SwordOfLordGwyn.png"),
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
                    BodySheet = P("Content", "Items", "Armor", "Melee", "ArtoriasArmor_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Melee", "ArtoriasGreaves_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Melee", "ArtoriasHelmet_Head.png"),
                    WeaponSprite = P("Content", "Items", "Weapons", "Melee", "Broadswords", "ArtoriasGreatsword.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1.1f,
                };
            }

            if (puppet.Equals("SoulOfCinder", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "SoulOfCinder",
                    BodySheet = P("Content", "Items", "Armor", "FirelinkArmor_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "FirelinkLeggings_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "FirelinkHelm_Head.png"),
                    WeaponSprite = P("Content", "Items", "Weapons", "Melee", "Broadswords", "SeveringDusk.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1.15f,
                };
            }

            if (puppet.Equals("DarkKnight", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "DarkKnight",
                    BodySheet = P("Content", "Items", "Armor", "Melee", "DarkKnightArmor_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Melee", "DarkKnightGreaves_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Melee", "DarkKnightHelmet_Head.png"),
                    // SwingPreview cannot load vanilla XNBs headlessly. Rune Blade has the same
                    // compact broadsword footprint and is used only as the offline pose proxy;
                    // the game draws the real vanilla Night's Edge selected by DarkKnight.cs.
                    WeaponSprite = P("Content", "Items", "Weapons", "Melee", "Broadswords", "RuneBlade.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            if (puppet.Equals("DarkBloodKnight", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "DarkBloodKnight",
                    BodySheet = P("Content", "Items", "Armor", "Melee", "DarkKnightArmor_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Melee", "DarkKnightGreaves_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Melee", "DarkKnightHelmet_Head.png"),
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
                    BodySheet = P("Content", "Items", "Armor", "Magic", "RedClothTunic_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Magic", "RedClothPants_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Magic", "RedClothHat_Head.png"),
                    WeaponSprite = P("Projectiles", "Enemy", "Weapons", "BeastClaw.png"),
                    OffHandWeaponSprite = P("Projectiles", "Enemy", "Weapons", "BeastClaw.png"),
                    OffHandWeaponScale = 0.5f,     // OolacileCultist.OffHandClawDrawScale
                    OffHandCarryRotation = 0.485f, // OolacileCultist.ClawCarryRotation (-0.30 + PiOver4)
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            if (puppet.Equals("AbysmalOolacileSorcerer", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "AbysmalOolacileSorcerer",
                    // The game dresses this boss in vanilla Spectre Robe/Pants + Plantera Mask, which this
                    // tool cannot load headlessly. Kahlrun's red cloth set is an offline body proxy only —
                    // judge the SWING from these renders, never the costume. The axe sprite is the real one.
                    BodySheet = P("Content", "Items", "Armor", "Magic", "RedClothTunic_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Magic", "RedClothPants_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Magic", "RedClothHat_Head.png"),
                    WeaponSprite = P("Projectiles", "Enemy", "Weapons", "GrandOolacileAxe.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                };
            }

            if (puppet.Equals("BlackNinja", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "BlackNinja",
                    // The game wears vanilla Ninja armor, which is packed in XNB files. The mod's
                    // Abyssal Ninja set is the closest offline rig proxy; the arm geometry is exact.
                    BodySheet = P("Content", "Items", "Armor", "AbyssalNinjaTop_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "AbyssalNinjaBottoms_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "AbyssalNinjaMask_Head.png"),
                    WeaponSprite = P("Content", "Projectiles", "Melee", "Flails", "DiamondCrusherBall.png"),
                    FlailBallSprite = P("Content", "Projectiles", "Melee", "Flails", "DiamondCrusherBall.png"),
                    FlailChainSprite = P("Content", "Projectiles", "Melee", "Flails", "DiamondCrusherBall_Chain.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                    ForceHideWeapon = true,
                };
            }

            if (puppet.Equals("DreadWraith", StringComparison.OrdinalIgnoreCase))
            {
                return new PuppetArt
                {
                    Name = "DreadWraith",
                    // Wills' set and Skeletron Mask are vanilla XNBs. Witchking supplies a similarly
                    // robed offline proxy while preserving the real composite-arm/body-row maths.
                    BodySheet = P("Content", "Items", "Armor", "Summon", "WitchkingRobe_Body.png"),
                    LegsSheet = P("Content", "Items", "Armor", "Summon", "WitchkingPants_Legs.png"),
                    HeadSheet = P("Content", "Items", "Armor", "Summon", "WitchkingHelmet_Head.png"),
                    WeaponSprite = P("Content", "Projectiles", "Enemy", "Weapons", "DreadWraithMaceBall.png"),
                    FlailBallSprite = P("Content", "Projectiles", "Enemy", "Weapons", "DreadWraithMaceBall.png"),
                    FlailChainSprite = P("Content", "Projectiles", "Enemy", "Weapons", "DreadWraithMaceChain.png"),
                    WeaponRotationOffset = 0f,
                    DrawScale = 1f,
                    ForceHideWeapon = true,
                };
            }

            return null;
        }
    }
}
