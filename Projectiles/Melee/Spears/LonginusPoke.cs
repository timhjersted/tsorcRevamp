using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusPoke : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 100f;
        public override float HoldoutRangeMax => 265f;
        public override float HitboxSize => 1f;
        public override float Scale => 1.2f;
        public override int dustID => -2; // skip ModdedSpearProjectile default dust

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.ownerHitCheck = false;
        }

        public override void CustomDust()
        {
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 90, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.2f);
            }
        }
    }
}
