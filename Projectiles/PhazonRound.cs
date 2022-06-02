using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PhazonRound : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.scale = 0.7f;
            Projectile.timeLeft = 240;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 1;
        }

        int storedDamage = 0;
        bool hasExploded = false;

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.PhazonContamination>(), 600);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
            Lighting.AddLight(Projectile.Center, Color.DarkBlue.ToVector3());

            if (storedDamage == 0)
            {
                Projectile.damage = (int)(0.9f * Projectile.damage);
                storedDamage = Projectile.damage;
            }

            if (Main.rand.Next(0, 4) == 0)
            {
                int speed = 2;
                //Dust.NewDustPerfect(projectile.position, 29, Vector2.Zero, 120, default, 2f).noGravity = true;
                Dust.NewDustPerfect(Projectile.position, DustID.FireworkFountain_Blue, (Projectile.velocity * 0.6f) + Main.rand.NextVector2Circular(speed, speed), 200, default, 0.8f).noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //Only the explosion does damage. Without this, both the impact *and* explosion do damage (annoying to balance, requires toning down explosion damage which makes it less useful)
            if (!hasExploded)
            {
                damage = 0;
            }
            else
            {
                damage = storedDamage;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            for (int i = 0; i < 2; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Circular(10, 10), ProjectileID.CrystalShard, Projectile.damage / 4, 0.5f, Projectile.owner);
            }

            Projectile.penetrate = 20;
            Projectile.width = 70;
            Projectile.height = 70;
            hasExploded = true;
            Projectile.Damage();

            int speed = 3;

            for (int i = 0; i < 10; i++)
            {
                //Dust.NewDustPerfect(projectile.position, 29, (projectile.velocity * 0.1f) + Main.rand.NextVector2Circular(speed, speed), 0, default, 3f).noGravity = true;
                Dust.NewDustPerfect(Projectile.position, DustID.FireworkFountain_Blue, (Projectile.velocity * Main.rand.NextFloat(0, 0.8f)) + Main.rand.NextVector2Circular(speed, speed), 200, default, 1.2f).noGravity = true;
            }
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PhazonRound];
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
