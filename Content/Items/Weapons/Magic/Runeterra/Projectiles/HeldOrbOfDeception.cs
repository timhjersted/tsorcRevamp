using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfDeception;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
{

    public class HeldOrbOfDeception : HeldRuneterraOrb
    {
        public override int Width => 50;
        public override int Height => 50;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfDeceptionSound)) + "_";
        public override int NotFilledDustID => DustID.MagicMirror;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 1;
        public override Color NotFilledColor => Color.LightSteelBlue;
        public override int OrbItemType => ModContent.ItemType<OrbOfDeception>();
        public override int ThrownOrbType => ModContent.ProjectileType<ThrownOrbOfDeception>();
        public override int FrameSpeed => 5;
        public override int SoundCooldown => 480;
        public override int DistanceToPlayerX => 34;
        public override int DistanceToPlayerY => 20;
        public override void DecideToKillOrb(Player player)
        {
            if (player.ownedProjectileCounts[ThrownOrbType] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<HeldOrbOfFlame>()] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<HeldOrbOfSpirituality>()] > 0 || player.dead)
            {
                Projectile.Kill();
            }
        }
    }
}