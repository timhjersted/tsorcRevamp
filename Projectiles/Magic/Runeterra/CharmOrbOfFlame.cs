using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Magic;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class CharmOrbOfFlame : CharmRuneterraOrb
    {
        public override int Width => 66;
        public override int Height => 28;
        public override float Scale => 1.3f;
        public override int CooldownType => ModContent.BuffType<OrbOfFlameFireballCooldown>();
        public override int DebuffType => ModContent.BuffType<Heatstroke>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfFlame/";
        public override Color LightColor => Color.Firebrick;
        public override int dustID => DustID.Torch;
    }
}