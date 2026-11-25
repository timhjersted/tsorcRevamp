using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.BloodLance
{
    public class BloodLanceItem : ModdedSpearItem
    {
        public const int OnHitHealChance = 50;
        public const int OnHitHealAmount = 3;
        public const int BurnDuration = 5;

        public override LocalizedText Tooltip =>
            base.Tooltip.WithFormatArgs(OnHitHealChance, OnHitHealAmount);
        public override int ProjectileID => ModContent.ProjectileType<BloodLanceProj>();
        public override int Width => 64;
        public override int Height => 64;
        public override int BaseDmg => 42;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 6.5f;
        public override int UseAnimationTime => 19;
        public override int UseTime => 19;
        public override int Rarity => ItemRarityID.Orange;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DarkLance);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 6000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
