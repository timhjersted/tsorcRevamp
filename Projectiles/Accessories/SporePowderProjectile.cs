using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Accessories
{
    public class SporePowderProjectile : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 210;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 210;
            Projectile.timeLeft = 210;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = 25;
            Projectile.ArmorPenetration = 20;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 240);
        }
    }
}