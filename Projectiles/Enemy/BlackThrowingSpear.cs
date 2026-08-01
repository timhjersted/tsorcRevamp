using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    class BlackThrowingSpear : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 2;
            AIType = 1;
            Projectile.hostile = true;
            Projectile.height = 14;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 1.0f;
            Projectile.tileCollide = true;
            Projectile.width = 14;
            Projectile.alpha = 0;
            Projectile.light = 1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(3))
            {
                int wraith = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.Wraith, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.5f);
                Main.dust[wraith].noGravity = true;

                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.DarkGray, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Projectile.ai[2] == 1f)
            {
                RedKnightVFX.DrawSpearWake(Projectile.Center - direction * 34f,
                    direction.ToRotation(), new Vector2(102f, 22f), 0.82f, empowered: true);
            }
            else
            {
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 34f,
                    direction.ToRotation(), new Vector2(92f, 20f), 0.72f);
            }
            return true;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 0;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0, 0, 0, default, 0.5f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0, 0, 0, default, 0.5f);
            }
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000, false);
            target.AddBuff(33, 300, false); //weak          
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit54, Projectile.Center); //death sound
                if (Projectile.ai[2] == 1f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                        (float)RedKnightBurstKind.SpearImpact, 1.15f);
                }
                else
                {
                    EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.BlackKnightSpearImpact);
                }
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height - 16), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), Projectile.damage, 3f, Projectile.owner);
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
            }
            Projectile.active = false;
        }
        #endregion
    }
}


