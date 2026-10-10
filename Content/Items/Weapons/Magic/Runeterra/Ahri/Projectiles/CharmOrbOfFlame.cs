using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Buffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfFlame;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles
{

    public class CharmOrbOfFlame : CharmRuneterraOrb
    {
        public override int Width => 66;
        public override int Height => 28;
        public override float Scale => 1.3f;
        public override int CooldownType => ModContent.BuffType<OrbOfFlameFireballCooldown>();
        public override int DebuffType => ModContent.BuffType<Heatstroke>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfFlameSound)) + "_";
        public override Color LightColor => Color.Firebrick;
        public override int dustID => DustID.Torch;
    }
}