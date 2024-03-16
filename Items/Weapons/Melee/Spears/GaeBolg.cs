using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class GaeBolg : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<GaeBolgProj>();
        public override int Width => 54;
        public override int Height => 54;
        public override int BaseDmg => 79;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 5.5f;
        public override int UseAnimationTime => 19;
        public override int UseTime => 19;
        public override int Rarity => ItemRarityID.LightPurple;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Gungnir);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
