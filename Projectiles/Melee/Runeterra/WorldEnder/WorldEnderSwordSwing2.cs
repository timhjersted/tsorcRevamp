using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder
{
    public class WorldEnderSwordSwing2 : WorldEnderSwordSwingBase
    {
        public override int Frames => 9;
        public override int Width => 272;
        public override int Height => 350;
        public override int Tier => 2;
        public override void CustomAI(Player player)
        {
            HitboxSize = new Vector2(80, 80);
            Hitbox1 = player.Center + (Velocity * 2.05f).RotatedBy(-0.15);
            Hitbox2 = player.Center + (Velocity * 2.05f).RotatedBy(0.15);
            Hitbox3 = player.Center + (Velocity * 2.25f).RotatedBy(-0.5);
            Hitbox4 = player.Center + (Velocity * 2.25f).RotatedBy(0.5);
            Hitbox5 = player.Center + (Velocity * 2.35f).RotatedBy(-0.7);
            Hitbox6 = player.Center + (Velocity * 2.35f).RotatedBy(0.7);
            Hitbox7 = player.Center + (Velocity * 1.35f).RotatedBy(0);
            Hitbox8 = player.Center + (Velocity * 1.4f).RotatedBy(0.55);
            Hitbox9 = player.Center + (Velocity * 1.4f).RotatedBy(-0.55);
            Hitbox10 = player.Center + (Velocity * 1.6f).RotatedBy(0.9);
            Hitbox11 = player.Center + (Velocity * 1.6f).RotatedBy(-0.9);
            Hitbox12 = player.Center + (Velocity * 0.6f).RotatedBy(0);
            Hitbox13 = player.Center + (Velocity * 0.75f).RotatedBy(0.9);
            Hitbox14 = player.Center + (Velocity * 0.75f).RotatedBy(-0.9);
            Hitbox15 = player.Center + (Velocity * 1.15f).RotatedBy(1.3);
            Hitbox16 = player.Center + (Velocity * 1.15f).RotatedBy(-1.3);
            /*
            Dust.NewDustPerfect(Hitbox1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox2, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox3, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox4, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox5, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox6, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox7, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox8, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox9, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox10, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox11, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox12, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox13, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox14, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox15, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox16, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(Hitbox17, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            */
            CritHitboxSize = new Vector2(85, 85);
            CritHitbox1 = player.Center + (Velocity * 2.2f).RotatedBy(-0.15);
            CritHitbox2 = player.Center + (Velocity * 2.2f).RotatedBy(0.15);
            CritHitbox3 = player.Center + (Velocity * 2.4f).RotatedBy(-0.5);
            CritHitbox4 = player.Center + (Velocity * 2.4f).RotatedBy(0.5);
            CritHitbox5 = player.Center + (Velocity * 2.5f).RotatedBy(-0.7);
            CritHitbox6 = player.Center + (Velocity * 2.5f).RotatedBy(0.7);
            /*
            Dust.NewDustPerfect(CritHitbox1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox2, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox3, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox4, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox5, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            */
        }
    }
}
