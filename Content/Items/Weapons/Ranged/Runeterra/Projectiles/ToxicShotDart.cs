using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.ToxicShot;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles
{
    public class ToxicShotDart : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 1;
        public override int DebuffType => ModContent.BuffType<VenomDebuff>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound));
    }
}