using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class AncientDragonLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<AncientDragonLanceProj>();
        public override int Width => 54;
        public override int Height => 52;
        public override int BaseDmg => 15;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 4;
        public override int UseAnimationTime => 25;
        public override int UseTime => 25;
        public override int Rarity => ItemRarityID.Blue;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public const int ArmorPen = 8;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPen);
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Trident);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
