using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class WingedSpear : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<WingedSpearProj>();
        public override int Width => 58;
        public override int Height => 58;
        public override int BaseDmg => 26;
        public override int BaseCritChance => 10;
        public override float BaseKnockback => 7;
        public override int UseAnimationTime => 20;
        public override int UseTime => 20;
        public override int Rarity => ItemRarityID.Green;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
    }
}
