using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class BlackThrowingSpear : ModProjectile
    {
        // Great Black Knight's aimed throw uses Owl Father's arrow flight. Other knight
        // throws retain the original vanilla thrown-projectile behavior.
        public const float ArrowFlightMode = 2f;

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 14;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            // Match BlackKnight's held spear exactly; previously the flying version was 0.1 larger.
            Projectile.scale = 0.9f;
            Projectile.tileCollide = true;
            Projectile.width = 14;
            Projectile.alpha = 0;
            Projectile.light = 1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.dedServ)
            {
                return;
            }
            if (Main.rand.NextBool(3))
            {
                int wraith = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.Wraith, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.5f);
                Main.dust[wraith].noGravity = true;

                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.DarkGray, 1.0f);
                Main.dust[dust].noGravity = true;
            }

            // Curse accent on every spear that builds curse (everything except Great Black Knight's Blight spear).
            bool blight = BlightBuildup.FromGreatBlackKnight(Projectile) || Projectile.ai[2] == ArrowFlightMode;
            if (!blight && Main.rand.NextBool(4))
            {
                CurseBuildup.SpawnAccentDust(Projectile.position, Projectile.width, Projectile.height, Projectile.velocity * 0.1f, 1.2f);
            }

            // Sparse plague dust at the tip, Black Knight / Great Black Knight only (ai[2] == 1 is
            // RedKnight's empowered reuse of this class — left untouched). Very low frequency by
            // design; this is a flavour trace, not the main dust trail above.
            if (Projectile.ai[2] != 1f && Main.rand.NextBool(20))
            {
                Vector2 tip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * (Projectile.height * 0.5f);
                int plagueDust;
                if (blight)
                {
                    int blightDustType = Main.rand.NextBool(6) ? DustID.YellowTorch : DustID.Wraith;
                    plagueDust = Dust.NewDust(tip, 2, 2, blightDustType, 0f, 0f, 150, default, 1.1f);
                }
                else if (Main.rand.NextBool(2))
                {
                    plagueDust = Dust.NewDust(tip, 2, 2, DustID.Smoke, 0f, 0f, 150, new Color(12, 8, 18), 1.4f);
                }
                else
                {
                    plagueDust = Dust.NewDust(tip, 2, 2, DustID.PurpleTorch, 0f, 0f, 150, new Color(120, 45, 170), 0.9f);
                }
                Main.dust[plagueDust].noGravity = true;
                Main.dust[plagueDust].velocity = Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.3f, 0.3f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Projectile.ai[2] == 1f)
            {
                // Was RedKnightVFX.DrawSpearWake; now the same grey wake the non-Red-Knight branch
                // below already uses, sized a touch smaller because this spear draws at 0.8 scale.
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 16f,
                    direction.ToRotation(), new Vector2(84f, 18f), 0.62f);
                Texture2D basicSpear = ModContent.Request<Texture2D>(
                    UsefulFunctions.RefactorableFilepath(typeof(BlackKnightSpear))).Value;
                Main.EntitySpriteDraw(basicSpear, Projectile.Center - Main.screenPosition, null,
                    lightColor, Projectile.rotation, basicSpear.Size() * 0.5f,
                    0.8f, SpriteEffects.None, 0f);
                return false;
            }
            else
            {
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 34f,
                    direction.ToRotation(), new Vector2(92f, 20f), 0.72f);
            }
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            bool blight = BlightBuildup.FromGreatBlackKnight(Projectile) || Projectile.ai[2] == ArrowFlightMode;
            if (blight)
            {
                BlightBuildup.Apply(target);
            }
            else
            {
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000, false);
            }
            target.AddBuff(BuffID.Weak, 300, false);
            Projectile.Kill();
        }

        #region Kill
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit54, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0f, 0f, 0, default, 0.5f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0f, 0f, 0, default, 0.5f);
            }

            if (Projectile.ai[2] == 1f)
            {
                // Red Knight family only. In hardmode the impact upgrades to the
                // DestinedDeathExplosion sheet plus red/black dust; pre-hardmode falls through
                // to the original burst completely unchanged.
                if (!DestinedDeathExplosion.TrySpawn(Projectile.GetSource_Death(),
                        Projectile.Center, 1.15f)
                    && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                        (float)RedKnightBurstKind.SpearImpact, 1.15f);
                }
            }
            else
            {
                EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.BlackKnightSpearImpact);

                // Black Knight / Great Black Knight only (this is the shared "else" branch;
                // RedKnight's ai[2]==1 reuse is above). The Great Black Knight leaves a half-radius
                // Blight cloud; the regular Black Knight leaves a 3-tile (24px radius), 60-tick Plague cloud.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    bool blight = BlightBuildup.FromGreatBlackKnight(Projectile) || Projectile.ai[2] == ArrowFlightMode;
                    float cloudRadius = VFX.PlagueTeleportCloud.SpearImpactCloudRadius;
                    int cloudLifetime = VFX.PlagueTeleportCloud.SpearImpactLifetimeTicks;
                    int cloudType = ModContent.ProjectileType<VFX.PlagueTeleportCloud>();
                    if (blight)
                    {
                        cloudRadius = VFX.PlagueTeleportCloud.MaxCloudRadius * 0.5f;
                        cloudLifetime = VFX.BlightTeleportCloud.LifetimeTicks;
                        cloudType = ModContent.ProjectileType<VFX.BlightTeleportCloud>();
                    }

                    Projectile cloud = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        cloudType, 0, 0f, Main.myPlayer, blight ? 0f : 1f, cloudRadius, blight ? 1f : 0f);
                    cloud.timeLeft = cloudLifetime;

                    // Six rotating, smoothly fading puffs scattered through the same radius.
                    // The palette follows the spear's source; neither teleport effect inherits them.
                    Projectile swarm = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<BlackKnightSpearBlightSwarm>(), 0, 0f, Main.myPlayer, cloudRadius, blight ? 1f : 0f);
                    swarm.timeLeft = cloudLifetime;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height - 16), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), Projectile.damage, 3f, Main.myPlayer);
            }

            Vector2 dustPosition = Projectile.position - Projectile.velocity;
            int firstDust = Dust.NewDust(dustPosition, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
            Main.dust[firstDust].noGravity = true;
            Main.dust[firstDust].velocity *= 2f;
            Dust.NewDust(dustPosition, Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 1f);
        }
        #endregion
    }
}

