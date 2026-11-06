using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynCinderTrail : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        // Wake geyser: for its first GeyserTicks each patch throws Fireball flames up off the floor beneath it.
        // The dash lays patches down one after another as it travels, so the geysers ripple along its path as a
        // wave, the way the Artorias smoke wave erupts along the floor behind its leading edge.
        const int GeyserTicks = 14;
        const int FloorSearchPixels = 96;
        float geyserFloorY = float.NaN;

        public override void AI()
        {
            float fade = Projectile.timeLeft / 70f;
            int age = 70 - Projectile.timeLeft;

            if (!Main.dedServ && age < GeyserTicks)
            {
                if (float.IsNaN(geyserFloorY))
                {
                    // The trail is laid at the dash's starting height; plant each geyser on the floor below it.
                    Vector2 probeStart = new Vector2(Projectile.Center.X - 2f, Projectile.Center.Y);
                    Vector2 drop = Collision.TileCollision(probeStart, new Vector2(0f, FloorSearchPixels), 4, 4, true, true);
                    geyserFloorY = probeStart.Y + drop.Y + 4f;
                }

                // Strongest at first, tapering off: 3 flames a tick down to 1.
                int count = 3 - age * 2 / GeyserTicks;
                float strength = 1f - age / (float)GeyserTicks;
                for (int i = 0; i < count; i++)
                {
                    Vector2 flamePosition = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-18f, 18f), geyserFloorY - Main.rand.NextFloat(0f, 6f));
                    Vector2 flameVelocity = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(2.5f, 5.5f) * (0.6f + 0.4f * strength));
                    float flameScale = Main.rand.NextFloat(1.4f, 2.1f) * (0.6f + 0.4f * strength);
                    int flameLife = Main.rand.Next(24, 34);
                    global::tsorcRevamp.VFX.VFX.FlameLobe(flamePosition, flameVelocity, flameScale, flameLife, 0.05f, global::tsorcRevamp.VFX.VFXParticleTexture.Fireball);
                }
            }
            if (Projectile.timeLeft < 20)
            {
                Projectile.damage = 0;
            }

            // A burning patch: rolling FireBreath flames lick upward and shrink as the patch dies, with a little
            // torch dust between them so the gaps never read as empty.
            if (Projectile.timeLeft % 3 == 0)
            {
                Vector2 flamePosition = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-22f, 22f), Projectile.Bottom.Y - Main.rand.NextFloat(6f, 18f));
                Vector2 flameVelocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.8f, -0.8f));
                float flameScale = MathHelper.Lerp(0.4f, 0.95f, fade);
                int flameLife = Main.rand.Next(22, 31);
                global::tsorcRevamp.VFX.VFX.FlameLobe(flamePosition, flameVelocity, flameScale, flameLife, 0.03f);
            }

            int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, -1.4f, 80, default, MathHelper.Lerp(0.6f, 1.3f, fade));
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity.X *= 0.35f;
            Main.dust[dust].velocity.Y -= 0.6f;
            Lighting.AddLight(Projectile.Center, 0.55f * fade, 0.25f * fade, 0.08f * fade);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 4 * 60);
        }
    }
}
