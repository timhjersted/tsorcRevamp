using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Short-range fire crescent thrown off the Hero's heavy AncientFireSword swings — gives the
    // heavy combo steps reach past the blade (the Souls "ranged melee" beat).  Dust-driven (no
    // sprite of its own), lives briefly, then fizzles.  Fire theme = OrangeRed / Torch dust.
    class HeroFireArc : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 22;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.1f;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.94f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(14f, 14f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, type, Projectile.velocity * 0.3f, 60, Color.OrangeRed, Main.rand.NextFloat(1.1f, 1.7f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 0.15f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 240);
        }
    }
}
