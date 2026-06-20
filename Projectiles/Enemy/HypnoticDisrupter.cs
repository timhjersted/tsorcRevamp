using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class HypnoticDisrupter : ModProjectile
    {

        public override void SetDefaults()
        {
            //projectile.aiStyle = 18;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hypnotic Disrupter");
            Main.projFrames[Type] = 6;
        }

        public override void AI()
        {




            if (Projectile.ai[1] > 0 && Projectile.timeLeft > 300)
            {
                Projectile.timeLeft = 300;
                Projectile.tileCollide = true;
            }
            Animate();

            Vector2 targetPosition = GetTargetPosition();

            if (targetPosition.X < Projectile.position.X)
            {
                if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
            }

            if (targetPosition.X > Projectile.position.X)
            {
                if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.2f;
            }

            if (targetPosition.Y < Projectile.position.Y)
            {
                if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
            }

            if (targetPosition.Y > Projectile.position.Y)
            {
                if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
            }

            FaceVelocity();


            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 50, color, 2.0f);
            Main.dust[dust].noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
            }
        }

        private void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
        }

        private void FaceVelocity()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            }
        }

        private Vector2 GetTargetPosition()
        {
            if (Projectile.ai[1] < -1f)
            {
                return new Vector2(Projectile.ai[0], -Projectile.ai[1]);
            }

            int targetPlayer = (int)Projectile.ai[0];
            if (targetPlayer < 0 || targetPlayer >= Main.maxPlayers || !Main.player[targetPlayer].active)
            {
                targetPlayer = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
            }
            return Main.player[targetPlayer].Center;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameCount = Main.projFrames[Type];
            int frameHeight = texture.Height / frameCount;
            Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffLengthMod = 1;
            if (!Main.expertMode) //surely that was the wrong way round
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.Bleeding, 600 / buffLengthMod, false); //bleeding
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600 / buffLengthMod, false); //you take knockback

            if (Projectile.ai[1] < 1)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 300 / buffLengthMod, false);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }
    }
}
