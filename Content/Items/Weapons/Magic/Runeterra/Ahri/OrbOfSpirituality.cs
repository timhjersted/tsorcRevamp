using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Buffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri
{
    public class OrbOfSpirituality : RuneterraOrb
    {
        public override int Width => 32;
        public override int Height => 32;
        public override int Damage => 300;
        public override int ManaCost => 60;
        public override int Rarity => ModContent.RarityType<DarkBlue>();
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 100000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
