using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class GaeBolgPoke : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 75f;
        public override float HoldoutRangeMax => 160f;
        public override float HitboxSize => 1;
        public override float Scale => 1.1f;
        public override int dustID => -2; // skip ModdedSpearProjectile default dust

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ownerHitCheck = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void CustomDust()
        {
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
