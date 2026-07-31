using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gravity of the Sun: a golden singularity at Gwyn's chest that RADIALLY drags every player
    ///toward him — the keystone anti-kite. Resistible by holding away or dodge-rolling, but strong
    ///enough to reel in a stationary caster, and it usually delivers you straight into his melee.
    ///Deals no damage itself; the sword waiting at the center is the payload. Sold by long light
    ///streams flowing into him. ai[0] = parent NPC whoAmI, ai[1] = duration in ticks.
    ///</summary>
    class GwynGravityWell : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float PullRadius = 760f;
        const float PullPerTick = 0.28f;
        const float PullSpeedCap = 6.5f;  //never accelerates a player beyond this toward him
        const float InnerDeadzone = 90f;  //no pull once you're already delivered
        const int SpiralSourceSize = 320;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> solarVortexEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> spiralCore;

        int ParentIndex => (int)Projectile.ai[0];
        int Duration => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 120;
        float Age => Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = (int)PullRadius;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
        }

        public override bool? CanDamage()
        {
            return false; //pure force — the greatsword at the center does the harming
        }

        public override void AI()
        {
            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (parent == null || !parent.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = parent.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            //Radial drag on every client (each client is authoritative for its own player)
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                Vector2 toCenter = parent.Center - player.Center;
                float dist = toCenter.Length();
                if (dist > PullRadius || dist < InnerDeadzone)
                {
                    continue;
                }
                Vector2 dir = toCenter.SafeNormalize(Vector2.Zero);
                //Only add pull while below the cap toward him — resisting stays possible
                float towardSpeed = Vector2.Dot(player.velocity, dir);
                if (towardSpeed < PullSpeedCap)
                {
                    player.velocity += dir * PullPerTick;
                }
            }

            //Long light streams flowing into the singularity + the golden core
            for (int i = 0; i < 5; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = parent.Center + ang.ToRotationVector2() * Main.rand.NextFloat(120f, PullRadius * 0.85f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.GoldCoin;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (parent.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(6f, 10f);
                Main.dust[dust].fadeIn = 0.3f;
            }
            int core = Dust.NewDust(parent.Center - new Vector2(10f), 20, 20, DustID.GoldFlame, 0f, 0f, 30, default, 1.7f);
            Main.dust[core].noGravity = true;
            Main.dust[core].velocity *= 0.1f;
            Lighting.AddLight(parent.Center, 1.2f, 1f, 0.4f);
        }

        static void LoadAssets()
        {
            solarVortexEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynSolarVortex", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            spiralCore ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float fadeIn = MathHelper.Clamp(Age / 18f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 16f, 0.3f, 1f);
            float opacity = fadeIn * fadeOut;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            DrawSpiralCore(drawPosition, opacity);
            DrawVortexField(drawPosition, opacity);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        void DrawSpiralCore(Vector2 drawPosition, float opacity)
        {
            Texture2D texture = spiralCore.Value;
            Rectangle source = new Rectangle(0, 0, SpiralSourceSize, SpiralSourceSize);
            float scale = InnerDeadzone * 2f / SpiralSourceSize;
            Main.EntitySpriteDraw(texture, drawPosition, source, new Color(255, 184, 48) * (opacity * 0.72f),
                -Main.GlobalTimeWrappedHourly * 0.82f, source.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        void DrawVortexField(Vector2 drawPosition, float opacity)
        {
            Effect effect = solarVortexEffect.Value;
            int fieldDiameter = (int)(PullRadius * 2f);
            Rectangle source = new Rectangle(0, 0, fieldDiameter, fieldDiameter);
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynSolarVortex"];
                effect.Parameters["BoundaryColor"].SetValue(new Color(255, 117, 16).ToVector3());
                effect.Parameters["StreamColor"].SetValue(new Color(255, 196, 62).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 244, 180).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["PullRadius"].SetValue(PullRadius);
                effect.Parameters["InnerRadius"].SetValue(InnerDeadzone);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, drawPosition, source, Color.White, 0f,
                    source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }
        }
    }
}
