using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class MarilithCataclysm : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cataclysmic Detonator");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 120;
            Projectile.hostile = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DD2BetsyFireball;

        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            for (int i = 5; i > 0; i--)
            {
                trailRotations[i] = trailRotations[i - 1];
            }
            trailRotations[0] = Projectile.rotation;

            for (int i = 0; i < 3; i++)
            {
                Vector2 dustSpeed = Main.rand.NextVector2Circular(4, 4);
                Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.InfernoFork, dustSpeed.X, dustSpeed.Y)].noGravity = true;
            }
            Player target = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    target = Main.player[i];
                    break;
                }
            }

            if (target != null)
            {
                float dist = Vector2.Distance(Projectile.Center, target.Center);
                if (dist < 256)
                {
                    Projectile.Kill();
                }
                float time = dist / 22;

                Vector2 targetVel = UsefulFunctions.Aim(Projectile.Center, target.Center + (target.velocity * time), 22);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVel, 0.013f);
            }

        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 70; i++)
            {
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, .1f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.Torch, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(Main.rand.NextVector2CircularEdge(15, 15), 130, Projectile.Center, 160, default, 1.5f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 1.5f).noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2CircularEdge(24, 24), Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), Projectile.damage, 0, Main.myPlayer);
            }
        }

        Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector2 offset = new Vector2(0, 0);

            //Draw shadow trails
            for (float i = ProjectileID.Sets.TrailCacheLength[Projectile.type] - 1; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, Projectile.oldPos[(int)i] - Main.screenPosition - offset, sourceRectangle, Color.OrangeRed * ((6 - i) / 6), trailRotations[(int)i], origin, Projectile.scale, SpriteEffects.None, 0);
            }

            //Draw actual npc
            Main.spriteBatch.Draw(texture, Projectile.position - Main.screenPosition - offset, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        Vector2 dustPos()
        {
            return Main.rand.NextVector2Circular(Projectile.width / 6, Projectile.height / 6) + Projectile.Center;
        }
        Vector2 dustVel()
        {
            return Main.rand.NextVector2Circular(7, 7);
        }
    }
}
