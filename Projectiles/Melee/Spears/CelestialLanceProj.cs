using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;


namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class CelestialLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 95f;
        public override float HoldoutRangeMax => 285f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.HallowedTorch;
        bool hasHealed = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(6) && !hasHealed)
            {
                player.statLife += CelestialLance.HealOnHit;
                player.HealEffect(CelestialLance.HealOnHit, true);
                hasHealed = true;
            }
        }
    }
}
