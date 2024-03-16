using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class AncientBloodLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<AncientBloodLanceProj>();
        public override int Width => 64;
        public override int Height => 64;
        public override int BaseDmg => 38;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 6.5f;
        public override int UseAnimationTime => 19;
        public override int UseTime => 19;
        public override int Rarity => ItemRarityID.Orange;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public const int BurnDuration = 5;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DarkLance);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
