using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    /// <summary>
    /// exists to give Swing usestyle weapons with no projectile the good autoswing aiming style
    /// i.e. instead of being locked to the direction with which you started the swing,
    /// or the direction youre walking, you can change swing directions by moving your cursor
    /// </summary>
    public class Nothing : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults() {
            Projectile.damage = 0;
            Projectile.timeLeft = 0;
            Projectile.height = Projectile.width = 1;
            Projectile.hostile = false;
            Projectile.friendly = false;
        }
    }
}
