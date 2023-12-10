using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class RadioactiveDart : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 3;
        public override int DebuffType => ModContent.BuffType<IrradiatedDebuff>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/";
        public override void CustomSetDefaults()
        {
            Projectile.light = 0.5f;
        }
    }
}