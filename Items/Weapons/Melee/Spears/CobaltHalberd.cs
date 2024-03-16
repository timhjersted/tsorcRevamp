using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{

    public class CobaltHalberd : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<CobaltHalberdProj>();
        public override int Width => 74;
        public override int Height => 74;
        public override int BaseDmg => 40;
        public override int BaseCritChance => 6;
        public override float BaseKnockback => 6;
        public override int UseAnimationTime => 26;
        public override int UseTime => 26;
        public override int Rarity => ItemRarityID.LightRed;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.CobaltBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
