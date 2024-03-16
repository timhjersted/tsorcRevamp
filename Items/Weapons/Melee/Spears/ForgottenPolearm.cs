using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class ForgottenPolearm : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<ForgottenPolearmProj>();
        public override int Width => 52;
        public override int Height => 52;
        public override int BaseDmg => 28;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 3;
        public override int UseAnimationTime => 22;
        public override int UseTime => 22;
        public override int Rarity => ItemRarityID.Orange;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
