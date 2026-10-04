using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra
{
    public class OrbOfFlame : RuneterraOrb
    {
        public override int Width => 32;
        public override int Height => 32;
        public override int Damage => 69;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
