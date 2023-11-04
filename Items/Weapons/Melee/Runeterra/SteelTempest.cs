using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Runeterra;

namespace tsorcRevamp.Items.Weapons.Melee.Runeterra
{
    public class SteelTempest : RuneterraKatanaItem
    {
        public override int DashCooldownBuffID => throw new System.NotImplementedException();
        public override int DashDustID => throw new System.NotImplementedException();
        public override int DashBuffID => throw new System.NotImplementedException();
        public override int SpinProjectileID => throw new System.NotImplementedException();
        public override int Tier => 1;
        public override float SwingSoundVolume => 0.15f;
        public override int RarityID => ItemRarityID.Green;
        public override int Value => Item.buyPrice(0, 10, 0, 0);
        public override int BaseDamage => 20;
        public override int ItemWidth => 86;
        public override int ItemHeight => 82;
        public override float ItemScale => 0.7f;
        public override float ItemKnockback => 3.5f;
        public override Color SlashColor => Color.WhiteSmoke;
        public override int TornadoReadyDustID => DustID.Smoke;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Melee/SteelTempest/";
        public override int ThrustCooldownBuffID => ModContent.BuffType<SteelTempestThrustCooldown>();
        public override int ThrustProjectileID => ModContent.ProjectileType<SteelTempestThrust>();
        public override string LocalizationPath => "Items.SteelTempest.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Katana);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}