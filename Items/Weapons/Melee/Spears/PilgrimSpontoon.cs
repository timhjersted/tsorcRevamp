using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class PilgrimSpontoon : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<PilgrimSpontoonProj>();
        public override int Width => 52;
        public override int Height => 52;
        public override int BaseDmg => 25;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 3;
        public override int UseAnimationTime => 20;
        public override int UseTime => 20;
        public override int Rarity => ItemRarityID.Green;
        public const int ArmorPen = 10;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPen);
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AncientDragonLance>());
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
