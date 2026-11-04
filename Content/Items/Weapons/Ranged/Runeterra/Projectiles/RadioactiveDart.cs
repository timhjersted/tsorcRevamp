using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles
{
    public class RadioactiveDart : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 3;
        public override int DebuffType => ModContent.BuffType<IrradiatedDebuff>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound));
        public override void CustomSetDefaults()
        {
            Projectile.light = 0.5f;
        }
    }
}