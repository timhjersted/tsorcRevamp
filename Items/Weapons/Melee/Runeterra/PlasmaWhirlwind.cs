using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class PlasmaWhirlwind : RuneterraKatanaItem
    {
        public override int DashCooldownBuffID => ModContent.BuffType<PlasmaWhirlwindDashCooldown>();
        public override int DashDustID => DustID.CoralTorch;
        public override int DashBuffID => ModContent.BuffType<PlasmaWhirlwindDash>();
        public override int SpinProjectileID => ModContent.ProjectileType<PlasmaWhirlwindSpin>();
        public override int Tier => 2;
        public override float SwingSoundVolume => 0.25f;
        public override int RarityID => ItemRarityID.LightPurple;
        public override int Value => Item.buyPrice(0, 30, 0, 0);
        public override int BaseDamage => 60;
        public override int ItemWidth => 70;
        public override int ItemHeight => 68;
        public override float ItemScale => 0.92f;
        public override float ItemKnockback => 4f;
        public override Color SlashColor => Color.Cyan;
        public override int TornadoReadyDustID => DustID.ApprenticeStorm;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/";
        public override int ThrustCooldownBuffID => ModContent.BuffType<PlasmaWhirlwindThrustCooldown>();
        public override int ThrustProjectileID => ModContent.ProjectileType<PlasmaWhirlwindThrust>();
        public override string LocalizationPath => "Items.PlasmaWhirlwind.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<SteelTempest>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}