using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellAbyssPoisonStrikeBall : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.light = 2;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 5 * 60, false);
            target.AddBuff(22, 600, false);
        }

        public override void AI()
        {
            if (Projectile.ai[2] == 1f)
            {
                if (Main.rand.NextBool(3))
                {
                    Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.CursedTorch,
                        -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.2f, 0.2f),
                        130, new Color(128, 190, 22), Main.rand.NextFloat(0.55f, 0.78f));
                    mote.noGravity = true;
                }
                for (int i = 0; i < 2; i++)
                {
                    Dust oldMote = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        DustID.YellowTorch, Main.rand.NextVector2Circular(0.35f, 0.35f),
                        120, default, 1.3f);
                    oldMote.noGravity = true;
                }
                return;
            }

            const int dustCount = 2;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.YellowTorch, Main.rand.NextVector2Circular(0.35f, 0.35f), 120, default, 1.3f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[2] == 1f)
            {
                RedKnightVFX.DrawPoisonOrb(Projectile.Center, Projectile.velocity, 0.96f);
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                    Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f,
                    Projectile.scale, SpriteEffects.None, 0f);
                return false;
            }
            return true;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 0;
            //
            //Terraria.Audio.SoundEngine.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 169, 0, 0, 0, default, 1f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 169, 0, 0, 0, default, 2f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 169, 0, 0, 0, default, 2f);
                Main.dust[dust].noGravity = true;
            }
            return true;
        }

        #region Kill
        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item100 with { Volume = 0.1f, Pitch = 0.09f }, Projectile.Center); // flame wall, lasts a bit longer than flame
                                                                                                                                            //Terraria.Audio.SoundEngine.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 10);
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 0, ModContent.ProjectileType<EnemySpellAbyssPoisonStrike>(), Projectile.damage, 1f, Projectile.owner);
                Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1394_1 = Projectile.width;
                int arg_1394_2 = Projectile.height;
                int arg_1394_3 = 15;
                float arg_1394_4 = 0f;
                float arg_1394_5 = 0f;
                int arg_1394_6 = 100;
                Color newColor = default(Color);
                int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
                Main.dust[num41].noGravity = true;
                Dust expr_13B1 = Main.dust[num41];
                expr_13B1.velocity *= 2f;
                Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1422_1 = Projectile.width;
                int arg_1422_2 = Projectile.height;
                int arg_1422_3 = 15;
                float arg_1422_4 = 0f;
                float arg_1422_5 = 0f;
                int arg_1422_6 = 100;
                newColor = default(Color);
                num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
                Main.dust[num41].noGravity = true;
            }
            Projectile.active = false;
        }
        #endregion
    }
}
