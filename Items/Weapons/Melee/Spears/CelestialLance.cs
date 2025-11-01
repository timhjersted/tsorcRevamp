using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class CelestialLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<CelestialLanceProj>();
        public override int Width => 44;
        public override int Height => 44;
        public override int BaseDmg => 345;
        public override int BaseCritChance => 4;
        public override float BaseKnockback => 10;
        public override int UseAnimationTime => 20;
        public override int UseTime => 20;
        public override int Rarity => ModContent.RarityType<OrangeRed>();
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public const float BonusDmgWhileFalling = 135f;
        public const int HealOnHit = 6;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BonusDmgWhileFalling, HealOnHit);
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
            {
                damage *= BonusDmgWhileFalling / 100f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Longinus>());
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 14);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>());
            recipe.AddIngredient(ItemID.FragmentStardust, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 140000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
