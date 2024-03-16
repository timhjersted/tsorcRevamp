
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class Longinus : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<LonginusProj>();
        public override int Width => 64;
        public override int Height => 64;
        public override int BaseDmg => 175;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 9;
        public override int UseAnimationTime => 20;
        public override int UseTime => 20;
        public override int Rarity => ItemRarityID.Cyan;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public const float BonusDmgWhileFalling = 130f;
        public const int HealOnHit = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BonusDmgWhileFalling, HealOnHit);
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
            {
                damage += BonusDmgWhileFalling / 100f;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ChlorophytePartisan, 1);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
