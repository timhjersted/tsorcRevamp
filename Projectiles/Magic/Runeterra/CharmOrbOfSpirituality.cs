using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class CharmOrbOfSpirituality : CharmRuneterraOrb
    {
        public override int Width => 40;
        public override int Height => 40;
        public override float Scale => 1.2f;
        public override int CooldownType => ModContent.BuffType<OrbOfSpiritualityCharmCooldown>();
        public override int DebuffType => ModContent.BuffType<Charmed>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/";
        public override Color LightColor => Color.Pink;
        public override int dustID => DustID.VenomStaff;
        public override void Rotation()
        {
            if (Projectile.velocity.X < 0)
            {
                Projectile.rotation -= MathHelper.Pi;
            }
            switch (Projectile.frame)
            {
                case 0:
                    {
                        FrameSpeed = 5;
                        break;
                    }
                case 1:
                    {
                        FrameSpeed = 4;
                        break;
                    }
                case 2:
                    {
                        FrameSpeed = 3;
                        break;
                    }
                case 3:
                    {
                        FrameSpeed = 2;
                        break;
                    }
                case 4:
                    {
                        FrameSpeed = 2;
                        break;
                    }
                case 5:
                    {
                        FrameSpeed = 2;
                        break;
                    }
                case 6:
                    {
                        FrameSpeed = 3;
                        break;
                    }
                case 7:
                    {
                        FrameSpeed = 4;
                        break;
                    }
            }
        }
    }
}