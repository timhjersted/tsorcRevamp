using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class ForgottenPearlSpear : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<ForgottenPearlSpearProj>();
        public override int Width => 58;
        public override int Height => 58;
        public override int BaseDmg => 20;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 7;
        public override int UseAnimationTime => 18;
        public override int UseTime => 18;
        public override int Rarity => ItemRarityID.Green;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
    }
}
