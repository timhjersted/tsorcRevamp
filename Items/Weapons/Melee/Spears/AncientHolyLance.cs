using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class AncientHolyLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<AncientHolyLanceProj>();
        public override int Width => 76;
        public override int Height => 76;
        public override int BaseDmg => 52;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 8.5f;
        public override int UseAnimationTime => 21;
        public override int UseTime => 21;
        public override int Rarity => ItemRarityID.LightRed;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHalberd);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
