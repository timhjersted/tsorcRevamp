using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class HiRyuuSpear : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<HiRyuuSpearProj>();
        public override int Width => 64;
        public override int Height => 64;
        public override int BaseDmg => 170;
        public override int BaseCritChance => 11;
        public override float BaseKnockback => 7;
        public override int UseAnimationTime => 15;
        public override int UseTime => 15;
        public override int Rarity => ItemRarityID.Yellow;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public static float HiRyuuSpearDamageBoost = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HiRyuuSpearDamageBoost);
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MonkStaffT2);
            recipe.AddIngredient(ItemID.SoulofFlight, 5);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.ObsidianSwordfish);
            recipe2.AddIngredient(ItemID.SoulofFlight, 5);
            recipe2.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
