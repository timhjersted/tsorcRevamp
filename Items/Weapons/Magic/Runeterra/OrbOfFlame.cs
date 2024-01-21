using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfFlame : RuneterraOrb
    {
        public override int Width => 32;
        public override int Height => 32;
        public override int Damage => 60;
        public override int ManaCost => 40;
        public override int Rarity => ItemRarityID.LightPurple;
        public override int Value => Item.buyPrice(0, 50, 0, 0);
        public override int HeldOrbProjectile => ModContent.ProjectileType<HeldOrbOfFlame>();
        public override int OrbProjectile => ModContent.ProjectileType<ThrownOrbOfFlame>();
        public override int FlameProjectile => ModContent.ProjectileType<FlameOrbOfFlame>();
        public override int CharmProjectile => ModContent.ProjectileType<CharmOrbOfFlame>();
        public override int CharmCooldownType => ModContent.BuffType<OrbOfFlameFireballCooldown>();
        public static Color FilledColor => Color.PaleVioletRed;
        public override int Tier => 2;
        public override string LocalizationPath => "Items.OrbOfFlame.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfDeception>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
