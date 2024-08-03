using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Items.Weapons.Magic.Runeterra
{
    public class OrbOfDeception : RuneterraOrb
    {
        public override int Width => 32;
        public override int Height => 32;
        public override int Damage => 18;
        public override int ManaCost => 20;
        public override int Rarity => ItemRarityID.Green;
        public override int Value => Item.buyPrice(0, 10, 0, 0);
        public override int HeldOrbProjectile => ModContent.ProjectileType<HeldOrbOfDeception>();
        public override int OrbProjectile => ModContent.ProjectileType<ThrownOrbOfDeception>();
        public override int FlameProjectile => ModContent.ProjectileType<FlameOrbOfDeception>();
        public override int CharmProjectile => throw new System.NotImplementedException();
        public override int CharmCooldownType => throw new System.NotImplementedException();
        public static Color FilledColor => Color.YellowGreen;
        public override int Tier => 1;
        public override string LocalizationPath => "Items.OrbOfDeception.";
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowOrb);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
