using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///A solar spear spawn-node (SolarLightningOrb.png, 4 frames): a crackling gold orb that hangs in
    ///the air as a telegraph, then fires a GwynSunlightSpear at the player and dissipates. The orb is
    ///the READ — you can see where every spear will come from before it fires. Used by the Sunlight
    ///Spears Volley (a few near Gwyn) and the Sunlight Spear Storm (a dozen, in waves).
    ///ai[0] = telegraph ticks before firing.
    ///</summary>
    class GwynSolarSpearNode : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynSolarSpearNode";

        const float SpearSpeed = 12f;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 20;
        int Timer => (int)Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + 8; // brief dissipate flourish after firing
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.4f, Pitch = 0.5f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return false; // the orb is a telegraph, not a hazard — only its spear hurts
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            //Crackle building toward the launch
            float progress = MathHelper.Min(1f, Timer / (float)TelegraphTicks);
            if (Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * (10f + progress * 14f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 60, default, 0.9f + progress);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (Projectile.Center - pos) * 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.4f + progress * 0.5f, 0.35f + progress * 0.4f, 0.15f);

            if (Timer == TelegraphTicks)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                    Vector2 dir = target != null && !target.dead
                        ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                        : Vector2.UnitY;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * SpearSpeed,
                        ModContent.ProjectileType<GwynSunlightSpear>(), Projectile.damage, 3f, Projectile.owner);
                }
                //Burst as the orb spends itself
                for (int i = 0; i < 14; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, vel.X, vel.Y, 40, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Timer > TelegraphTicks)
            {
                return false; // orb spent; the dissipate dust carries the tail
            }
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Timer * 0.4f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, 0f, new Vector2(texture.Width / 2f, frameHeight / 2f), 0.5f * pulse, SpriteEffects.None, 0);
            return false;
        }
    }
}
