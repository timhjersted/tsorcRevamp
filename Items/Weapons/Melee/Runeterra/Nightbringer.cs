using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class Nightbringer : RuneterraKatanaItem
    {
        public override int DashCooldownBuffID => ModContent.BuffType<NightbringerDashCooldown>();
        public override int DashDustID => DustID.Torch;
        public override int DashBuffID => ModContent.BuffType<NightbringerDash>();
        public override int SpinProjectileID => ModContent.ProjectileType<NightbringerSpin>();
        public override int Tier => 3;
        public override float SwingSoundVolume => 0.3f;
        public override int RarityID => ItemRarityID.Red;
        public override int Value => Item.buyPrice(1, 0, 0, 0);
        public override int BaseDamage => 250;
        public override int ItemWidth => 94;
        public override int ItemHeight => 110;
        public override float ItemScale => 1f;
        public override float ItemKnockback => 5f;
        public override Color SlashColor => Color.DarkOrange;
        public override int TornadoReadyDustID => DustID.DesertTorch;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/";
        public override int ThrustCooldownBuffID => ModContent.BuffType<NightbringerThrustCooldown>();
        public override int ThrustProjectileID => ModContent.ProjectileType<NightbringerThrust>();
        public override string LocalizationPath => "Items.Nightbringer.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<PlasmaWhirlwind>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}