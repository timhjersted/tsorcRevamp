using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's hurled greatsword (shares the SwordOfGwyn item art): spins across the arena trailing
    ///fire, arcs at its apex, and returns to his hand — a full-lane horizontal wall that punishes
    ///edge-campers. While it flies, Gwyn is weaponless (the punish window if you're close).
    ///ai[0] = owner NPC whoAmI, ai[1] = launch-locked outbound distance in pixels,
    ///ai[2] = 1 once the return begins.
    ///</summary>
    class GwynGreatswordBoomerang : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SwordOfGwyn";

        const float ReturnAccel = 1.1f;
        const float ReturnTopSpeed = 19f;
        const float CatchRange = 52f;
        const float OutboundStallDistance = 180f;
        const float OutboundStallSpeed = 8f;
        public const float TargetOvershoot = 300f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderTrailEffect;
        static Asset<Texture2D> flowNoise;

        int ParentIndex => (int)Projectile.ai[0];
        float OutboundDistance => Projectile.ai[1] > 0f ? Projectile.ai[1] : 750f;
        bool Returning => Projectile.ai[2] == 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false; // a god's flaming blade doesn't stop for terrain
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 720;
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            Projectile.rotation += 0.38f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (Returning)
            {
                if (parent == null || !parent.active)
                {
                    Projectile.Kill();
                    return;
                }
                //Accelerating home to his hand
                Vector2 toHand = parent.Center - Projectile.Center;
                if (toHand.Length() < CatchRange)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
                    Projectile.Kill();
                    return;
                }
                Vector2 desired = toHand.SafeNormalize(Vector2.UnitX) * ReturnTopSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
                if (Projectile.velocity.Length() < ReturnTopSpeed)
                {
                    Projectile.velocity += toHand.SafeNormalize(Vector2.Zero) * ReturnAccel * 0.25f;
                }
                Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 30); // never expire mid-return
            }
            else
            {
                // Distance, rather than a timer, owns the turn. ai[1] is locked by Gwyn at release
                // to the target's distance + 300px, so movement after the tell cannot shorten the
                // promised overshoot. Constant speed carries the blade across the expanded arena;
                // only the final 180px eases down to preserve the old readable apex stall.
                float remaining = OutboundDistance - Projectile.localAI[0];
                if (remaining <= 0f)
                {
                    Projectile.ai[2] = 1f;
                    Projectile.netUpdate = true;
                }
                else if (remaining < OutboundStallDistance)
                {
                    float stallProgress = 1f - remaining / OutboundStallDistance;
                    float speed = MathHelper.Lerp(Projectile.velocity.Length(), OutboundStallSpeed,
                        stallProgress * 0.08f);
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
                }

                // Clamp the last outbound step to the promised turn point. This keeps the centre of
                // the blade at least 300px past the locked target instead of reversing one tick early.
                remaining = OutboundDistance - Projectile.localAI[0];
                float outboundStep = System.Math.Min(Projectile.velocity.Length(), remaining);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * outboundStep;
                Projectile.localAI[0] += outboundStep;
                if (Projectile.localAI[0] >= OutboundDistance)
                {
                    Projectile.ai[2] = 1f;
                    Projectile.netUpdate = true;
                }
            }

            //Blazing spin trail
            for (int i = 0; i < 3; i++)
            {
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 60, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1f, 1f);
            }
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0.15f);

            if (Main.rand.NextBool(14))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.35f, Pitch = -0.2f }, Projectile.Center);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        static void LoadAssets()
        {
            cinderTrailEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderTrailEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = flowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynCinderBlade"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 48, 5).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 137, 18).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 230, 150).ToVector3());
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(texture.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(texture.Size());
                Vector2 pixelDrawSize = texture.Size() * 0.85f;
                Vector2 pixelCount = new Vector2(
                    System.Math.Max(1f, pixelDrawSize.X / 2f),
                    System.Math.Max(1f, pixelDrawSize.Y / 2f));
                effect.Parameters["PixelGrid"].SetValue(new Vector4(
                    pixelCount.X, pixelCount.Y, 1f / pixelCount.X, 1f / pixelCount.Y));
                effect.Parameters["Progress"].SetValue(Returning ? 1f : 0f);

                for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float trailStrength = 1f - i / (float)Projectile.oldPos.Length;
                    effect.Parameters["Opacity"].SetValue(trailStrength * 0.34f);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(texture, trailPosition, null, Color.White, Projectile.oldRot[i],
                        origin, 0.85f, SpriteEffects.None, 0);
                }

                effect.Parameters["Opacity"].SetValue(0.48f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                    Projectile.rotation, origin, 0.85f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation,
                origin, 0.85f, SpriteEffects.None, 0);
            return false;
        }
    }
}
