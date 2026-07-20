using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Gravemaw
{
    ///<summary>
    ///Gravemaw Orb: a slow soul-orb that drags nearby enemies toward it (a pocket black hole), then
    ///BURSTS on impact/expiry into a friendly Soul Nova ring. The player's take on the boss's gravity
    ///well. Reuses the PurpleSkull sprite, drawn larger. Deals light contact damage while travelling.
    ///</summary>
    class GravemawOrb : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/PurpleSkull";

        const float PullRadius = 240f;

        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.scale = 1.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (++Projectile.frameCounter >= 6) { Projectile.frameCounter = 0; Projectile.frame = (Projectile.frame + 1) % 4; }
            Projectile.rotation += 0.2f;
            Projectile.velocity *= 0.985f; // slows as it flies

            // Drag nearby enemies toward the orb.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this) || npc.boss || npc.knockBackResist <= 0f) continue;
                Vector2 to = Projectile.Center - npc.Center;
                float dist = to.Length();
                if (dist > PullRadius || dist < 8f) continue;
                npc.velocity += to.SafeNormalize(Vector2.Zero) * 0.35f * npc.knockBackResist;
            }

            // Swirling event-horizon dust.
            for (int i = 0; i < 3; i++)
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + a.ToRotationVector2() * Main.rand.NextFloat(30f, PullRadius * 0.7f);
                int d = Dust.NewDust(pos, 4, 4, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, 0f, 0f, 100, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = (Projectile.Center - pos).SafeNormalize(Vector2.Zero) * 5f;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.15f, 0.8f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                int d = Dust.NewDust(Projectile.Center - new Vector2(10f), 20, 20, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, vel.X, vel.Y, 60, default, 1.6f);
                Main.dust[d].noGravity = true;
            }
            if (Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<GravemawNova>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 200f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            int frameH = tex.Height / Main.projFrames[Type];
            Rectangle src = new Rectangle(0, Projectile.frame * frameH, tex.Width, frameH);
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, src, new Color(180, 120, 220),
                Projectile.rotation, src.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
