using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Pets;

namespace tsorcRevamp.Projectiles
{
    class tsorcGlobalProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile projectile)
        {
            if (projectile.owner < Main.maxPlayers && Main.player[projectile.owner].active)
            {
                Player player = Main.player[projectile.owner];
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaCrescent>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.Next(2) == 0)
                    {
                        int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 164, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = false;
                    }
                }

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaNew>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.Next(2) == 0)
                    {
                        int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 57, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 130, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                if (projectile.owner == Main.myPlayer && !projectile.hostile && projectile.DamageType == DamageClass.Melee)
                {
                    if (modPlayer.MagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.45f);
                        for (int i = 0; i < 4; i++)
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                    }

                    if (modPlayer.GreatMagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.55f);
                        for (int i = 0; i < 4; i++)
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 172, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 172, player.velocity.X * .2f, player.velocity.Y * 0.2f, 30, default(Color), 1.3f);
                            Main.dust[dust].noGravity = true;
                        }
                    }

                    if (modPlayer.CrystalMagicWeapon)
                    {
                        Lighting.AddLight(projectile.position, 0.3f, 0.3f, 0.55f);
                        for (int i = 0; i < 2; i++)
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 221, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 68, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 30, default(Color), .9f);
                            Main.dust[dust].noGravity = true;
                        }
                        {
                            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 172, player.velocity.X * .2f, player.velocity.Y * 0.2f, 30, default(Color), 1.3f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }

                if (projectile.owner == Main.myPlayer && (projectile.aiStyle == 99 || projectile.aiStyle == 15 || projectile.type == ModContent.ProjectileType<Projectiles.SilverBall>()
                    || projectile.type == ModContent.ProjectileType<Projectiles.MythrilBall>() || projectile.type == ModContent.ProjectileType<AdamantiteBall>()
                    || projectile.type == ModContent.ProjectileType<Projectiles.HeavenBall>()) && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 1)
                {
                    //projectile.Kill();

                    if (projectile.aiStyle == 99)
                    {
                        projectile.ai[0] = -1; //return yoyo smoothly, dont just kill it. This took me ages to find :(
                    }
                    else
                    {
                        projectile.ai[1] = 1; //return flail smoothly, dont just kill it
                    }
                }
            }

            if (projectile.type == ProjectileID.IceBolt || projectile.type == ProjectileID.EnchantedBeam || projectile.type == ProjectileID.SwordBeam || projectile.type == ProjectileID.FrostBoltSword
                || projectile.type == ProjectileID.LightBeam || projectile.type == ProjectileID.NightBeam || projectile.type == ProjectileID.TerraBeam)
            {
                projectile.ai[1]++;
                if (projectile.ai[1] > 15)
                {
                    projectile.timeLeft = 0;
                }
            }


            //Destroyer shoots true lasers instead of normal projectile lasers
            //Probe lasers are replaced with true lasers. This is actually an enormous nerf because they were not telegraphed and were hard to see before.
            if (NPC.AnyNPCs(NPCID.TheDestroyer))
            {
                if (projectile.type == ProjectileID.DeathLaser)
                {
                    projectile.Kill();
                }
                if (projectile.type == ProjectileID.PinkLaser)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), projectile.Center, projectile.velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 20, 0, Main.myPlayer, -2, projectile.owner);
                    projectile.Kill();
                }
            }
            return true;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damage, float knockback, bool crit)
        {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaCrescent>())
            {
                target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 180); // Adds the ExampleJavelin debuff for a very small DoT
            }

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaNew>())
            {
                target.AddBuff(BuffID.Midas, 300);
            }

            if (projectile.owner == Main.myPlayer && (modPlayer.MagicWeapon || modPlayer.GreatMagicWeapon) && projectile.DamageType == DamageClass.Melee)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit44.WithVolume(0.3f), target.position);
            }
            if (projectile.owner == Main.myPlayer && modPlayer.CrystalMagicWeapon && projectile.DamageType == DamageClass.Melee)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27.WithVolume(0.3f), target.position);
            }
        }

        public override void OnHitPlayer(Projectile projectile, Player target, int damage, bool crit)
        {
            if (projectile.type == ProjectileID.EyeLaser && projectile.ai[0] == 1)
            {
                target.AddBuff(BuffID.Slow, 180);
            }

            if (projectile.type == ProjectileID.DemonSickle)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 15);
                target.AddBuff(BuffID.Slow, 180);
                target.AddBuff(BuffID.Darkness, 180);

            }

            base.OnHitPlayer(projectile, target, damage, crit);
        }

        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            if (projectile.type == ProjectileID.SandBallFalling && projectile.velocity.X != 0)
            {
                return false;
            }

            else
            {
                return true;
            }
        }
    }
}
