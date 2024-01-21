using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Accessories
{
    public class FriendlyIonBomb : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.timeLeft = DetonationTime;
            Projectile.hostile = false;
            Projectile.friendly = true;
        }

        int DetonationTime = 60;
        float DetonationProgress = 0;
        float DetonationPercent
        {
            get => 1f - (DetonationProgress / DetonationTime);
        }

        NPC targetNPC;
        public override void AI()
        {
            Projectile.velocity *= 0.95f;
            DetonationProgress++;
            Projectile.rotation++;

            if (DetonationProgress == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Projectile.owner, 240, 25);

                int? targetIndex = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
                if (targetIndex.HasValue)
                {
                    targetNPC = Main.npc[targetIndex.Value];
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), targetNPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Projectile.owner, 120, 25);
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), targetNPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Projectile.owner, 240, 25);
                }
                else
                {
                    Projectile.Center = new Vector2(0, 0);
                    Projectile.Kill();
                }
            }

            if (targetNPC != null)
            {
                Projectile.Center = targetNPC.Center;
                Projectile.netUpdate = true;
            }
        }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.Distance(targetHitbox.Center.ToVector2()) < 230 && DetonationTime == DetonationProgress + 1)
            {
                return true;
            }
            return false;
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
        }

        public override bool PreKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom") with { Volume = 0.5f }, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Projectile.owner, 400, 30);
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(28, 28);
                Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Blue, dustVel, 200, Color.White, 1.3f).noGravity = true;
            }
            return true;
        }

        public static Effect RingEffect;
        public static Effect CoreEffect;
        public override bool PreDraw(ref Color lightColor)
        {

            //if (RingEffect == null)
            {
                RingEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/LightningLine", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle ringRectangle2 = new Rectangle(0, 0, 1200, 1200);
            Vector2 ringOrigin2 = ringRectangle2.Size() / 2f;

            RingEffect.Parameters["textureToSizeRatio"].SetValue(tsorcRevamp.NoiseVoronoi.Size() / ringRectangle2.Size());
            RingEffect.Parameters["shaderColor"].SetValue(Color.Blue.ToVector3());
            RingEffect.Parameters["active"].SetValue(-1);

            RingEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, Projectile.Center - Main.screenPosition, ringRectangle2, Color.White, 0, ringOrigin2, 1, SpriteEffects.None);
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;

        }
    }
}
