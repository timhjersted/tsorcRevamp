
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Graphics.Shaders;

namespace tsorcRevamp.Projectiles.VFX
{
    class BossSelectVisuals : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999;
        }

        public override void AI()
        {
            Main.player[Projectile.owner].mouseInterface = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}