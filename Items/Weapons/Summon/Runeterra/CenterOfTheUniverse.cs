using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.Dragons;

namespace tsorcRevamp.Items.Weapons.Summon.Runeterra
{
    public class CenterOfTheUniverse : RuneterraGauntlets
    {
        public static List<CenterOfTheUniverseStar> projectiles = null;
        public static int processedProjectilesCount = 0;
        public override float SoundVolumeAbstract => 1f;
        public static float SoundVolume;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/";
        public override int Damage => 160;
        public override float Knockback => 6f;
        public override int Width => 48;
        public override int Height => 56;
        public override int Value => Item.buyPrice(1, 0, 0, 0);
        public override int Rarity => ItemRarityID.Red;
        public override int BuffType => ModContent.BuffType<CenterOfTheUniverseBuff>();
        public override int ProjectileType => ModContent.ProjectileType<CenterOfTheUniverseStar>();
        public override int DragonType => ModContent.ProjectileType<StarForger>();
        public override Vector3 HoldItemLight => new Vector3(0.1f, 0.08f, 0.05f);
        public override string LocalizationPath => "Items.CenterOfTheUniverse.";
        public override int Tier => 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ScorchingPoint.BallSummonTagDmgMult, ScorchingPoint.DragonSummonTagDmgMult);
        public override void CustomSetDefaults()
        {
            projectiles = new List<CenterOfTheUniverseStar>() { };
            SoundVolume = SoundVolumeAbstract;
        }
        public override bool? UseItem(Player player)
        {
            if (Dragon != null)
            {
                if (Main.mouseRight && player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount >= 0 && Dragon.type == ModContent.ProjectileType<StarForger>())
                {
                    (Dragon.ModProjectile as RuneterraDragon).StartAltSequence();
                    player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount = 0;
                }
            }
            return base.UseItem(player);
        }
        public static void ReposeProjectiles(Player player)
        {
            // repose projectiles relatively to the first one so they are evenly spread on the radial circumference
            List<CenterOfTheUniverseStar> projectileList = new List<CenterOfTheUniverseStar>();
            processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<CenterOfTheUniverseStar>()];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<CenterOfTheUniverseStar>() && Main.projectile[i].owner == player.whoAmI)
                {
                    projectileList.Add((CenterOfTheUniverseStar)Main.projectile[i].ModProjectile);
                }
            }

            for (int i = 1; i < processedProjectilesCount; ++i)
            {
                projectileList[i].currentAngle = projectileList[i - 1].currentAngle + 2f * (float)Math.PI / processedProjectilesCount;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<InterstellarVesselGauntlet>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}