using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class HeldOrbOfFlame : HeldRuneterraOrb
    {
        public override int Width => 54;
        public override int Height => 54;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfFlame/";
        public override int NotFilledDustID => DustID.Torch;
        public override int FilledDustID => DustID.RedTorch;
        public override int Tier => 2;
        public override Color NotFilledColor => Color.Firebrick;
        public override int OrbItemType => ModContent.ItemType<OrbOfFlame>();
        public override int ThrownOrbType => ModContent.ProjectileType<ThrownOrbOfFlame>();
        public override int FrameSpeed => 7;
        public override int SoundCooldown => 300;
        public override int DistanceToPlayerX => 38;
        public override int DistanceToPlayerY => 20;
        public override void DecideToKillOrb(Player player)
        {
            if (player.ownedProjectileCounts[ThrownOrbType] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<HeldOrbOfSpirituality>()] > 0 || player.dead)
            {
                Projectile.Kill();
            }
        }
    }
}