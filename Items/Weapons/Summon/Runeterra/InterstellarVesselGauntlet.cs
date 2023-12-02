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
    [LegacyName("InterstellarVesselControls")]
    public class InterstellarVesselGauntlet : RuneterraGauntlets
    {
        public static List<InterstellarVesselShip> projectiles = null;
        public static int processedProjectilesCount = 0;
        public override float SoundVolumeAbstract => 0.5f;
        public static float SoundVolume;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/";
        public override int Damage => 60;
        public override float Knockback => 5f;
        public override int Width => 30;
        public override int Height => 34;
        public override int Value => Item.buyPrice(0, 30, 0, 0);
        public override int Rarity => ItemRarityID.LightPurple;
        public override int BuffType => ModContent.BuffType<InterstellarCommander>();
        public override int ProjectileType => ModContent.ProjectileType<InterstellarVesselShip>();
        public override int DragonType => ModContent.ProjectileType<ASOL13>();
        public override Vector3 HoldItemLight => new Vector3(0.1f, 0.08f, 0.05f);
        public override string LocalizationPath => "Items.InterstellarVesselGauntlet.";
        public override int Tier => 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ScorchingPoint.BallSummonTagDmgMult, ScorchingPoint.DragonSummonTagDmgMult);
        public override void CustomSetDefaults()
        {
            projectiles = new List<InterstellarVesselShip>() { };
            SoundVolume = SoundVolumeAbstract;
        }
        public override void CustomShoot(Projectile proj)
        {
            projectiles.Add((InterstellarVesselShip)proj.ModProjectile);
        }
        public static void ReposeProjectiles(Player player)
        {
            // repose projectiles relatively to the first one so they are evenly spread on the radial circumference
            List<InterstellarVesselShip> projectileList = new List<InterstellarVesselShip>();
            processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<InterstellarVesselShip>()];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<InterstellarVesselShip>() && Main.projectile[i].owner == player.whoAmI)
                {
                    projectileList.Add((InterstellarVesselShip)Main.projectile[i].ModProjectile);
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

            recipe.AddIngredient(ModContent.ItemType<ScorchingPoint>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}