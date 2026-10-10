using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Bases;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Projectiles;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.SteelTempest;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo
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
        public override int BaseDamage => 24;
        public override int ItemWidth => 86;
        public override int ItemHeight => 82;
        public const float BaseScale = 0.7f; public override float ItemScale => BaseScale;
        public override float ItemKnockback => 3.5f;
        public override Color SlashColor => Color.WhiteSmoke;
        public override int TornadoReadyDustID => DustID.Smoke;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(SteelTempestSound));
        public override int ThrustCooldownBuffID => ModContent.BuffType<SteelTempestThrustCooldown>();
        public override int ThrustProjectileID => ModContent.ProjectileType<SteelTempestThrust>();
        public override string LocalizationPath => "Items.SteelTempest.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Katana);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 10000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}