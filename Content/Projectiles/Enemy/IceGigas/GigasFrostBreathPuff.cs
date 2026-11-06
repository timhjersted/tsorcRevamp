using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.IceGigas
{
    // Traveling puffs own both shader and dust: old puffs keep their earlier sweep angles.
    class GigasFrostBreathPuff : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        const int Lifetime = 80;
        const float Decay = 0.978f;
        //Frost Breath's puffs (ai[0] = 1) reach 33% farther: ~340px → ~450px at its 9px/t launch. The reach comes
        //from a slower decay over 20 more ticks, not a faster launch, so it still drifts like breath. Undertow's
        //point-blank exhale (ai[0] = 0) keeps the short form. The lifetime is set on the first AI tick, not OnSpawn,
        //because OnSpawn only runs on the spawning machine and timeLeft isn't synced.
        const int LongLifetime = 100;
        const float LongDecay = 0.984f;
        public const float LongBreathFlag = 1f;

        static int LifetimeOf(Projectile puff)
        {
            if (puff.ai[0] == LongBreathFlag)
            {
                return LongLifetime;
            }

            return Lifetime;
        }
        readonly Dust[] carriedDust = new Dust[48];
        int nextDust;
        static Asset<Effect> breathEffect;
        static Asset<Texture2D> mistNoise, detailNoise;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 48;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = Projectile.height = 20;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.light = 0.2f;
        }

        // Shared by the breath and frost releases: white snow flecks over frost mist.
        internal static Dust SpawnFrostDust(Vector2 position, Vector2 velocity, float scale, bool white)
        {
            Dust dust = Dust.NewDustPerfect(position, white ? DustID.Snow : DustID.Frost, velocity, 80, default, scale);
            dust.noGravity = true;
            return dust;
        }

        public override void AI()
        {
            bool longBreath = Projectile.ai[0] == LongBreathFlag;

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = LifetimeOf(Projectile);
            }

            float decay = Decay;

            if (longBreath)
            {
                decay = LongDecay;
            }

            Projectile.velocity *= decay;
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.dedServ) return;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            // Move dust with its own cloud. customData detects slots recycled by Terraria.
            foreach (Dust dust in carriedDust)
            {
                if (dust == null || !dust.active || dust.customData != this) continue;
                dust.position += Projectile.velocity;
                dust.velocity = Vector2.Zero;
                dust.alpha = 80 + (int)(175f * (1f - fade));
            }
            if (Projectile.timeLeft <= 8) return;
            float age = 1f - Projectile.timeLeft / (float)LifetimeOf(Projectile);
            for (int i = 0; i < 2; i++)
            {
                Dust oldDust = carriedDust[nextDust];
                if (oldDust != null && oldDust.customData == this) oldDust.active = false;
                Dust dust = SpawnFrostDust(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    Vector2.Zero, 1.05f + age * 0.3f, i == 0);
                dust.customData = this;
                carriedDust[nextDust] = dust;
                nextDust = (nextDust + 1) % carriedDust.Length;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Chilled, 2 * 60);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = 0f;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = 0f;
            return false; // Wash along walls with shader and dust still attached.
        }

        public override void OnKill(int timeLeft)
        {
            foreach (Dust dust in carriedDust)
                if (dust != null && dust.customData == this) dust.active = false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

        public override bool PreDraw(ref Color lightColor)
        {
            // One batch transition for ALL puffs, including recovery and Undertow's exhale.
            for (int i = 0; i < Projectile.whoAmI; i++)
                if (Main.projectile[i].active && Main.projectile[i].type == Type && IsVisible(Main.projectile[i])) return false;
            breathEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/IceGigasFrostBreath", AssetRequestMode.ImmediateLoad);
            mistNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            Effect effect = breathEffect.Value;
            effect.CurrentTechnique = effect.Techniques["IceGigasFrostBreath"];
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture oldTexture = device.Textures[1];
            SamplerState oldSampler = device.SamplerStates[1];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                device.Textures[1] = detailNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                effect.Parameters["OuterColor"].SetValue(new Color(18, 40, 68).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(88, 168, 226).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(205, 236, 250).ToVector3());
                effect.Parameters["CoreMix"].SetValue(0.72f);
                effect.Parameters["BodyAlphaGain"].SetValue(1f);
                effect.Parameters["EmissiveGain"].SetValue(0.027f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile puff = Main.projectile[i];
                    if (!puff.active || puff.type != Type || !IsVisible(puff)) continue;
                    float fade = MathHelper.Clamp(puff.timeLeft / 12f, 0f, 1f);
                    float age = 1f - puff.timeLeft / (float)LifetimeOf(puff);
                    Vector2 size = new Vector2(56f, 64f) * (1f + age * 0.4f);
                    Vector2 blocks = size * 0.5f;
                    effect.Parameters["Progress"].SetValue(fade);
                    effect.Parameters["AlphaScale"].SetValue(fade * 0.3f);
                    effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(mistNoise.Value, puff.Center - Main.screenPosition, null, Color.White,
                        puff.rotation, mistNoise.Value.Size() * 0.5f, size / mistNoise.Value.Size(), SpriteEffects.None, 0f);
                }
            }
            finally
            {
                device.Textures[1] = oldTexture;
                device.SamplerStates[1] = oldSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
            return false;
        }

        static bool IsVisible(Projectile puff)
        {
            const int padding = 48; // Same culling test as Main.DrawProj, including this type's fluff.
            Rectangle view = new Rectangle((int)Main.Camera.ScaledPosition.X - padding,
                (int)Main.Camera.ScaledPosition.Y - padding,
                (int)Main.Camera.ScaledSize.X + padding * 2, (int)Main.Camera.ScaledSize.Y + padding * 2);
            return view.Intersects(puff.Hitbox);
        }
    }
}
