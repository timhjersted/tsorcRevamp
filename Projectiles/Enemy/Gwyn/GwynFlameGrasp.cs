using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Lord's Grasp: a hand of the First Flame that reaches out and seizes. A shader-defined
    ///flaming claw flies toward the player; on contact it immolates them (big damage, heavy On Fire!,
    ///hard knockback — the "seize and hurl"). Bypasses the comfort of range: it's the shield-turtle /
    ///spacing punish. ai[0] = parent NPC whoAmI (unused reach ref).
    ///</summary>
    class GwynFlameGrasp : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int Lifetime = 55;
        const int ShaderDrawSize = 58;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> flameGraspEffect;
        static Asset<Texture2D> flameTexture;
        static Asset<Texture2D> smoothNoise;

        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime; // the reach window
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.99f;
            //A grasping claw of fire — five "fingers" of flame trailing the reach
            for (int i = 0; i < 6; i++)
            {
                Vector2 spread = Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.Center + spread - new Vector2(2f), 4, 4, type, 0f, 0f, 40, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.15f;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 0.15f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //Seized and immolated
            target.AddBuff(BuffID.OnFire, 10 * 60);
            target.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 8f - new Vector2(0f, 4f);
        }

        static void LoadAssets()
        {
            flameGraspEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynFlameGrasp", AssetRequestMode.ImmediateLoad);
            flameTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_AuraC12", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            Rectangle source = new Rectangle(0, 0, ShaderDrawSize, ShaderDrawSize);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fadeIn = MathHelper.Clamp((Age + 2f) / 5f, 0.55f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 5f, 0f, 1f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = flameGraspEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = smoothNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynFlameGrasp"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 38, 3).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 118, 13).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 229, 144).ToVector3());
                effect.Parameters["Opacity"].SetValue(fadeIn * fadeOut);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["PrimaryTextureSize"].SetValue(flameTexture.Value.Size());
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(flameTexture.Value, drawPosition, source, Color.White,
                    Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y, 40, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
