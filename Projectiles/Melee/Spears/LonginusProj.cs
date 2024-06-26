using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 85f;
        public override float HoldoutRangeMax => 265f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.GemAmber;
        bool hasHealed = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(6) && !hasHealed)
            {
                player.statLife += Longinus.HealOnHit;
                player.HealEffect(Longinus.HealOnHit, true);
                hasHealed = true;
            }
        }
    }

}
