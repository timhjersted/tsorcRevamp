using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    class OldHalberd : ModdedSpearItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Main.LocalPlayer.GetTotalDamage(DamageClass.Melee).ApplyTo(BaseDmg));
        public override int ProjectileID => ModContent.ProjectileType<OldHalberdProj>();
        public override int Width => 60;
        public override int Height => 60;
        public override int BaseDmg => 35;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 6;
        public override int UseAnimationTime => 29;
        public override int UseTime => 29;
        public override int Rarity => ItemRarityID.White;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void HoldItem(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().OldWeapon = true;
        }
    }
}
