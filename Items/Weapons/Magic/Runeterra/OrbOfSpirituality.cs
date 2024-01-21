using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfSpirituality : RuneterraOrb
    {
        public override int Width => 32;
        public override int Height => 32;
        public override int Damage => 330;
        public override int ManaCost => 60;
        public override int Rarity => ItemRarityID.Red;
        public override int Value => Item.buyPrice(1, 0, 0, 0);
        public override int HeldOrbProjectile => ModContent.ProjectileType<HeldOrbOfSpirituality>();
        public override int OrbProjectile => ModContent.ProjectileType<ThrownOrbOfSpirituality>();
        public override int FlameProjectile => ModContent.ProjectileType<FlameOrbOfSpirituality>();
        public override int CharmProjectile => ModContent.ProjectileType<CharmOrbOfSpirituality>();
        public override int CharmCooldownType => ModContent.BuffType<OrbOfSpiritualityCharmCooldown>();
        public static Color FilledColor => Color.YellowGreen;
        public override int Tier => 3;
        public override string LocalizationPath => "Items.OrbOfSpirituality.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<OrbOfFlame>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
