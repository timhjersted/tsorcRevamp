using Microsoft.Xna.Framework;
using System.Collections.Generic;
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

                if (modPlayer.WaspPower & projectile.type == ProjectileID.HornetStinger)
                {
                    projectile.penetrate = 6;
                    projectile.usesLocalNPCImmunity = true;
                    projectile.localNPCHitCooldown = 10;
                    projectile.extraUpdates = 5;
                } 
                else if (!modPlayer.WaspPower & projectile.type == ProjectileID.HornetStinger)
                {
                    projectile.penetrate = 1;
                    projectile.usesLocalNPCImmunity = false;
                }

                if (projectile.type == ProjectileID.BloodArrow)
                {
                    projectile.tileCollide = false;
                    projectile.timeLeft = 60;
                }

                if (projectile.type == ProjectileID.FrostBlastFriendly)
                {
                    
                    projectile.penetrate = 6;
                    projectile.usesLocalNPCImmunity = true;
                    projectile.localNPCHitCooldown = 10;
                    projectile.extraUpdates = 3;
                }

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaCrescent>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.NextBool(2))
                    {
                        int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 164, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = false;
                    }
                }

                if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaNew>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>()))
                {
                    if (Main.rand.NextBool(2))
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

                if (projectile.owner == Main.myPlayer && (projectile.aiStyle == ProjAIStyleID.Flail || projectile.aiStyle == ProjAIStyleID.Yoyo || projectile.type == ModContent.ProjectileType<Projectiles.Flails.BerserkerNightmareBall>()
                    || projectile.type == ModContent.ProjectileType<Projectiles.Flails.HeavensTearBall>() || projectile.type == ModContent.ProjectileType<Flails.SunderingLightBall>() || projectile.type == ModContent.ProjectileType<Projectiles.Flails.MoonfuryBall>()
                    ) && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 1)
                {
                    //projectile.Kill();

                    if (projectile.aiStyle == ProjAIStyleID.Yoyo)
                    {
                        projectile.ai[0] = -1; //return yoyo smoothly, dont just kill it. This took me ages to find :( (doesn't work)
                    }
                    else if (projectile.aiStyle == ProjAIStyleID.Flail)
                    {
                        projectile.ai[1] = 1; //return flail smoothly, dont just kill it(doesn't work)
                    }
                    else projectile.Kill();
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
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, projectile.velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -2, projectile.owner);
                    }
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit44 with { Volume = 0.3f }, target.position);
            }
            if (projectile.owner == Main.myPlayer && modPlayer.CrystalMagicWeapon && projectile.DamageType == DamageClass.Melee)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f }, target.position);
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
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.player[projectile.owner];
            Vector2 LeatherTip = new Vector2(10, 18) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 SnapTip = new Vector2(22, 26) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 SpinalTip = new Vector2(14, 18) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 CoolTip = new Vector2(14, 24) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 FireTip = new Vector2(18, 26) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 DurenTip = new Vector2(10, 16) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 MorningTip = new Vector2(14, 14) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 DarkTip = new Vector2(28, 20) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            Vector2 KaleidoTip = new Vector2(14, 30) * player.whipRangeMultiplier * projectile.WhipSettings.RangeMultiplier;
            List<Vector2> points = projectile.WhipPointsForCollision;
            if (projectile.type == ProjectileID.BlandWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], LeatherTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], LeatherTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.ThornWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], SnapTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], SnapTip).Intersects(target.Hitbox))
                {
                    crit = true; 
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.BoneWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], SpinalTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], SpinalTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.CoolWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], CoolTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], CoolTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.FireWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], FireTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], FireTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.SwordWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], DurenTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], DurenTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.MaceWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], MorningTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], MorningTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.ScytheWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], DarkTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], DarkTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
            if (projectile.type == ProjectileID.RainbowWhip)
            {
                if (Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 2], KaleidoTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(projectile.WhipPointsForCollision[points.Count - 1], KaleidoTip).Intersects(target.Hitbox))
                {
                    crit = true;
                    if (player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250)
                    {
                        damage *= 5;
                        damage /= 4;
                    }
                }
            }
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
