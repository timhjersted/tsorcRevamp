using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class DragoonLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<DragoonLanceProj>();
        public override int Width => 74;
        public override int Height => 74;
        public override int BaseDmg => 140;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 15;
        public override int UseAnimationTime => 17;
        public override int UseTime => 17;
        public override int Rarity => ItemRarityID.Yellow;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GaeBolg>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ItemID.SoulofFright, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 1);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
