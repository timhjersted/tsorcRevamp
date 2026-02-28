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
        public override int BaseDmg => 185;
        public override int BaseCritChance => 16;
        public override float BaseKnockback => 7;
        public override int UseAnimationTime => 15;
        public override int UseTime => 15;
        public override int Rarity => ItemRarityID.Red;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public static float HiRyuuSpearDamageBoost = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HiRyuuSpearDamageBoost);
    }
}
