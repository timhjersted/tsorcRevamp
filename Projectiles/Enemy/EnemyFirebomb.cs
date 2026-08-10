using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyFirebomb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.knockBack = 9;
            Projectile.scale = 1f;
            Projectile.light = 1f;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
        }
        /// <summary>
        /// Explosion damage. THIS WAS THE BUG behind "the Great Red Knight's bomb hits like a
        /// prehardmode projectile": the explosion used to OVERWRITE Projectile.damage with a flat
        /// 18 / 28 / 38 by world tier, discarding whatever the thrower spawned it with entirely —
        /// so a Super Hard Mode boss's bomb did 38 no matter how the boss was scaled, while its
        /// spear (45+) and great attack (50+) both went through SHMScale.
        ///
        /// The tier values are kept as a FLOOR so every other user of this projectile (and any
        /// caller that passes 0) behaves exactly as before; a thrower that asks for more now gets it.
        /// </summary>
        static int ExplosionDamage(int requestedDamage)
        {
            int floor = 18;
            if (Main.hardMode)
            {
                floor = 28;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                floor = 38;
            }
            return System.Math.Max(requestedDamage, floor);
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            Projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            Projectile.netUpdate = true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 1) //the one frame make the explosion only deal damage once.
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 1 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = ExplosionDamage(Projectile.damage);
                Projectile.knockBack = 9f;
                Projectile.DamageType = DamageClass.Throwing;
                Projectile.netUpdate = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // If colliding with a ceiling (moving upward and Y velocity was stopped/reversed by tile collision), bounce off ceiling rather than exploding
            if (oldVelocity.Y < -0.5f && Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = System.Math.Abs(oldVelocity.Y) * 0.4f;
                if (Projectile.velocity.X != oldVelocity.X)
                {
                    Projectile.velocity.X = oldVelocity.X * 0.8f;
                }
                return false;
            }
            Projectile.timeLeft = 2;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[2] == 1f && Projectile.timeLeft > 2)
            {
                float progress = MathHelper.Clamp((240f - Projectile.timeLeft) / 210f, 0f, 1f);
                Vector2 fusePoint = Projectile.Center + new Vector2(3f, -8f).RotatedBy(Projectile.rotation);
                RedKnightVFX.DrawBombFuse(fusePoint, progress, planted: false);
            }
            return true;
        }
        public override void AI()
        {
            if (/*projectile.owner == Main.myPlayer &&*/ Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = ExplosionDamage(Projectile.damage);
                Projectile.knockBack = 9f;
                Projectile.DamageType = DamageClass.Throwing;
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Projectile.ai[2] != 1f && Main.rand.NextBool(2))
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;

                    for (int i = 0; i < 2; i++)
                    {
                        int pink = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.CrystalSerpent, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.3f);
                        Main.dust[pink].noGravity = true;
                    }
                    // Main.dust[dustIndex].position = projectile.Center + new Vector2(0f, (float)(-(float)projectile.height / 2)).RotatedBy((double)projectile.rotation, default(Vector2)) * 1.1f;
                    /*dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;*/
                    // Main.dust[dustIndex].position = projectile.Center + new Vector2(0f, (float)(-(float)projectile.height / 2 - 6)).RotatedBy((double)projectile.rotation, default(Vector2)) * 1.1f;
                }
            }
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                    {
                        Projectile.velocity.X = Projectile.velocity.X * 0.99f;
                    }
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            return;
        }

        public override void OnKill(int timeLeft)
        {
            // Play explosion sound
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { PitchVariance = 0.5f }, Projectile.Center);

            if (Projectile.ai[2] == 1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                    (float)RedKnightBurstKind.BombExplosionLayered, 1f);
            }

            bool knightBomb = Projectile.ai[2] == 1f;

            // Knight bombs use a shader burst with a 120px footprint, matching their actual explosion hitbox.
            // Count up / scale down for knight bombs (28 @ 2.0 -> 52 @ 1.45): the same amount of ink,
            // but small enough that the cloud stops reading as 16px blocks. Non-knight firebombs keep
            // their original 200 @ 2.0 so every other enemy that throws one is unaffected.
            int dustCount = knightBomb ? 52 : 200;
            float dustScale = knightBomb ? 1.45f : 2f;
            for (int i = 0; i < dustCount; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 36, Projectile.position.Y + 36), Projectile.width - 74, Projectile.height - 74, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), dustScale);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= knightBomb ? 2.4f : 3.5f;
            }

            if (knightBomb && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int blazeDamage = ExplosionDamage(Projectile.damage);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (MathHelper.TwoPi / 8f);
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 blazeVel = dir * 0.56f;
                    int proj = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, blazeVel,
                        ModContent.ProjectileType<EnemySpellBlaze>(), blazeDamage, 5f, Main.myPlayer);
                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].timeLeft = 90;
                    }
                }
            }

            // Knight bombs only: red/soot fire on top of the grey smoke above, built as the three
            // distinct layers a blast needs (§20) rather than one pass of identical motes.
            if (knightBomb && !Main.dedServ)
            {
                // MIDGROUND — the body of the fireball. 80 motes of RedTorch, reach 68px, scale 0.75-1.5, noGravity.
                for (int i = 0; i < 80; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    bool soot = i % 4 == 0;
                    Dust ember = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(4f, 68f),
                        soot ? DustID.Shadowflame : DustID.RedTorch,
                        direction * Main.rand.NextFloat(1.6f, 6.8f),
                        soot ? 150 : 90,
                        soot ? new Color(16, 3, 8) : new Color(214, 22, 42),
                        Main.rand.NextFloat(0.75f, 1.5f));
                    ember.noGravity = true;
                }

                // FOREGROUND — 35 tiny fast sparks that outrun the body and die quickly.
                for (int i = 0; i < 35; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Dust spark = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(6f, 26f),
                        DustID.Torch,
                        direction * Main.rand.NextFloat(7f, 12.5f),
                        60, new Color(255, 176, 96),
                        Main.rand.NextFloat(0.45f, 0.85f));
                    spark.noGravity = true;
                }

                // BACKGROUND — 16 slow, dark, gravity-bound smoke motes that linger after the fire
                // has gone. They outlive the 44t shader and stop the blast ending on a hard cut.
                for (int i = 0; i < 16; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Dust smoke = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(10f, 46f),
                        DustID.Smoke,
                        direction * Main.rand.NextFloat(0.6f, 2.2f) - Vector2.UnitY * 0.7f,
                        170, new Color(38, 30, 32),
                        Main.rand.NextFloat(0.5f, 0.8f));
                    smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.7f);
                }
            }

            // Large Smoke Gore spawn. Knight bombs get two more cloud sprites than before (1 -> 3
            // bursts, i.e. 6 clouds) so the longer-lived shader has smoke to sit inside.
            int smokeBursts = knightBomb ? 3 : 2;
            for (int g = 0; g < smokeBursts; g++)
            {
                if (!Main.dedServ)
                {
                    int goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 1f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                    goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 1f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                }
            }
            // reset size to normal width and height.
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
        }
    }
}
