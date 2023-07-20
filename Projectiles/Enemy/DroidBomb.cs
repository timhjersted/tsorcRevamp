using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Projectiles.Enemy
{
    class DroidBomb : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.scale = 1f;
            Projectile.timeLeft = 360;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.damage = 45;
            Projectile.knockBack = 9;
        }


        int timer = 0;

        public override void AI()
        {
            if (timer > 60 && timer < 300)
            {
                Player closest = UsefulFunctions.GetClosestPlayer(Projectile.Center);

                if (closest != null)
                {
                    UsefulFunctions.SmoothHoming(Projectile, closest.Center, 0.3f, 20, closest.velocity, false);
                }

                if (!Main.dedServ)
                {
                    Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));
                    Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));
                    Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));
                }
            }
            else
            {
                Projectile.velocity *= 0.95f;
            }

            timer++;
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // setup projectile for explosion
            Projectile.damage = Projectile.damage * 2;
            Projectile.penetrate = 20;
            Projectile.width = Projectile.width << 3;
            Projectile.height = Projectile.height << 3;

            Projectile.Damage();

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 0, Main.myPlayer, 400, 30);
            }

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.Torch, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.FireworkFountain_Red, velX, velY, 160, default, 1.5f);
            }

            // terminate projectile
            Projectile.active = false;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.immune[Projectile.owner] > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 4 * 60);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 4 * 60);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.Draw(((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]), Projectile.Center - Main.screenPosition, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Frame(), Color.White, 0, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Size() / 2, 2, SpriteEffects.None, 0);
            return false;
        }
    }
}
