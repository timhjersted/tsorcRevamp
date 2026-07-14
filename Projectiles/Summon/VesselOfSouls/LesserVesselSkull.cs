using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.VesselOfSouls
{
    ///<summary>The Lesser Vessel minion's soul-skull: a friendly, weakly-homing summon projectile.
    ///Reuses the PurpleSkull sprite. ai[0] = homing factor.</summary>
    class LesserVesselSkull : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/PurpleSkull";

        float Homing => Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (++Projectile.frameCounter >= 6) { Projectile.frameCounter = 0; Projectile.frame = (Projectile.frame + 1) % 4; }

            if (Homing > 0f)
            {
                NPC t = FindTarget();
                if (t != null)
                {
                    float speed = Projectile.velocity.Length();
                    Vector2 desired = (t.Center - Projectile.Center).SafeNormalize(Projectile.velocity) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, Homing);
                }
            }
            Projectile.rotation = 0f; // upright skull; flip (not rotate) so it never inverts
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 130, default, 0.9f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.08f, 0.4f);
        }

        NPC FindTarget()
        {
            NPC closest = null;
            float best = 700f * 700f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this)) continue;
                float dSq = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (dSq < best) { best = dSq; closest = npc; }
            }
            return closest;
        }

        // Drawn UPRIGHT (no rotation) so it never appears upside down; just mirrored to face travel L/R.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            int frameH = tex.Height / Main.projFrames[Type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameH, tex.Width, frameH);
            SpriteEffects fx = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, lightColor,
                0f, src.Size() / 2f, Projectile.scale, fx, 0);
            return false;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Muzzle burst at the Vessel's mouth as the skull leaves it.
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f) + Projectile.velocity * 0.15f;
                int d = Dust.NewDust(Projectile.Center - new Vector2(8f), 16, 16, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 100, default, Main.rand.NextFloat(1f, 1.5f));
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Purple soul-burst on impact.
            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4.5f, 4.5f);
                int d = Dust.NewDust(Projectile.Center - new Vector2(10f), 20, 20, Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 90, default, Main.rand.NextFloat(1.1f, 1.8f));
                Main.dust[d].noGravity = true;
            }
        }
    }
}
