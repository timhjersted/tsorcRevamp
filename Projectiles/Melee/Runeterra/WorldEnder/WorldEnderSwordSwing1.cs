using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Projectiles.Melee.Runeterra.WorldEnder
{
    public class WorldEnderSwordSwing1 : WorldEnderSwordSwingBase
    {
        public override int Frames => 9;
        public override int Width => 290;
        public override int Height => 102;
        public override int Tier => 1;
        public override void CustomAI(Player player)
        {
            HitboxSize = new Vector2(50, 50);
            Hitbox1 = player.Center + (Velocity * 1.77f).RotatedBy(-0.075);
            Hitbox2 = player.Center + (Velocity * 1.77f).RotatedBy(0.075);
            Hitbox3 = player.Center + (Velocity * 1.45f).RotatedBy(-0.075);
            Hitbox4 = player.Center + (Velocity * 1.45f).RotatedBy(0.075);
            Hitbox5 = player.Center + (Velocity * 1.13f).RotatedBy(-0.075);
            Hitbox6 = player.Center + (Velocity * 1.13f).RotatedBy(0.075);
            Hitbox7 = player.Center + (Velocity * 0.81f).RotatedBy(-0.095);
            Hitbox8 = player.Center + (Velocity * 0.81f).RotatedBy(0.095);
            Hitbox9 = player.Center + (Velocity * 0.49f).RotatedBy(-0.2);
            Hitbox10 = player.Center + (Velocity * 0.49f).RotatedBy(0.2);
            Hitbox11 = player.Center + (Velocity * 0.17f).RotatedBy(-0.6);
            Hitbox12 = player.Center + (Velocity * 0.17f).RotatedBy(0.6);
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
            */
            CritHitboxSize = new Vector2(55, 55);
            CritHitbox1 = player.Center + (Velocity * 1.86f).RotatedBy(-0.075);
            CritHitbox2 = player.Center + (Velocity * 1.86f).RotatedBy(0.075);
            CritHitbox3 = player.Center + (Velocity * 1.6f).RotatedBy(-0.075);
            CritHitbox4 = player.Center + (Velocity * 1.6f).RotatedBy(0.075);
            /*
            Dust.NewDustPerfect(CritHitbox1, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox2, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox3, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            Dust.NewDustPerfect(CritHitbox4, DustID.MartianSaucerSpark, null, 0, default, 0.5f);
            */
        }
    }
}
