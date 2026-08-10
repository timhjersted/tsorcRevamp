using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            if (Main.rand.NextBool(3))
            {
                int wraith = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.Wraith, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.5f);
                Main.dust[wraith].noGravity = true;

                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 50, Color.DarkGray, 1.0f);
                Main.dust[dust].noGravity = true;
            }

            // Sparse plague dust at the tip, Black Knight / Great Black Knight only (ai[2] == 1 is
            // RedKnight's empowered reuse of this class — left untouched). Very low frequency by
            // design; this is a flavour trace, not the main dust trail above.
            if (Projectile.ai[2] != 1f && Main.rand.NextBool(20))
            {
                Vector2 tip = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * (Projectile.height * 0.5f);
                int plagueDust = Main.rand.NextBool(2)
                    ? Dust.NewDust(tip, 2, 2, DustID.Smoke, 0f, 0f, 150, new Color(12, 8, 18), 1.4f)
                    : Dust.NewDust(tip, 2, 2, DustID.PurpleTorch, 0f, 0f, 150, new Color(120, 45, 170), 0.9f);
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
                    "tsorcRevamp/Projectiles/Enemy/BlackKnightSpear").Value;
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
                    // RedKnight's ai[2]==1 reuse is above). Reuses the plague-teleport cloud at half
                    // its usual radius — same dust + CurseBuildup application, just smaller. Fires on
                    // both a player hit and a tile hit, since OnKill runs for either (penetrate
                    // reaches 0 after a player hit; the default OnTileCollide kills on tile impact).
                    // Melee never runs this: the melee jab/spear-hitbox classes are separate and don't
                    // touch this file at all.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float cloudRadius = VFX.PlagueTeleportCloud.MaxCloudRadius * 0.5f;
                        var cloud = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                            ModContent.ProjectileType<VFX.PlagueTeleportCloud>(), 0, 0f, Main.myPlayer,
                            1f, cloudRadius);
                        cloud.timeLeft = VFX.PlagueTeleportCloud.LifetimeTicks;

                        // Six rotating, smoothly fading blight-cloud puffs scattered through the same
                        // radius — purely decorative, kept off PlagueTeleportCloud itself so it
                        // doesn't leak into this knight's (and others') teleport effects.
                        Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                            ModContent.ProjectileType<BlackKnightSpearBlightSwarm>(), 0, 0f, Main.myPlayer, cloudRadius);
                    }
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

