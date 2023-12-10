using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;
using tsorcRevamp.Projectiles.Summon.Runeterra.Dragons;

namespace tsorcRevamp.Items.Weapons.Summon.Runeterra
{
    public class ScorchingPoint : RuneterraGauntlets
    {
        public static List<ScorchingPointFireball> projectiles = null;
        public static int processedProjectilesCount = 0;
        public override float SoundVolumeAbstract => 0.35f;
        public static float SoundVolume;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/ScorchingPoint/";
        public override int Damage => 20;
        public override float Knockback => 3f;
        public override int Width => 32;
        public override int Height => 34;
        public override int Value => Item.buyPrice(0, 10, 0, 0);
        public override int Rarity => ItemRarityID.Green;
        public override int BuffType => ModContent.BuffType<CenterOfTheHeat>();
        public override int ProjectileType => ModContent.ProjectileType<ScorchingPointFireball>();
        public override int DragonType => ModContent.ProjectileType<AshenLord>();
        public override Vector3 HoldItemLight => new Vector3(0.1f, 0.08f, 0.05f);
        public override string LocalizationPath => "Items.ScorchingPoint.";
        public override int Tier => 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BallSummonTagDmgMult, DragonSummonTagDmgMult);
        public override void CustomSetDefaults()
        {
            projectiles = new List<ScorchingPointFireball>() { };
            SoundVolume = SoundVolumeAbstract;
        }
        public override void CustomShoot(Projectile proj)
        {
            projectiles.Add((ScorchingPointFireball)proj.ModProjectile);
        }
        public static void ReposeProjectiles(Player player)
        {
            // repose projectiles relatively to the first one so they are evenly spread on the radial circumference
            List<ScorchingPointFireball> projectileList = new List<ScorchingPointFireball>();
            processedProjectilesCount = player.ownedProjectileCounts[ModContent.ProjectileType<ScorchingPointFireball>()];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<ScorchingPointFireball>() && Main.projectile[i].owner == player.whoAmI)
                {
                    projectileList.Add((ScorchingPointFireball)Main.projectile[i].ModProjectile);
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

            recipe.AddIngredient(ItemID.FeralClaws);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}