using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public class GhostOfAHollowWarrior : ModNPC, IStaggerable //Don't look at the code, it's muy malo. Look at Lothric Spear Knight for a better example code management-wise
    {
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;

        int shieldFrame;
        int shieldAnimTimer;

        // now has damage scaling
        public int smallSlashDamage = 10;
        public int bigSlashDamage = 15;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 17;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0f; //Is ghost
            NPC.aiStyle = -1;
            NPC.damage = 18;
            NPC.defense = 10;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 100;
            NPC.value = 500;
            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 25;
                NPC.damage = 28;
                NPC.value = 1250;
                smallSlashDamage = 20;
                bigSlashDamage = 25;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 45;
                NPC.damage = 38;
                NPC.value = 4000;
                smallSlashDamage = 30;
                bigSlashDamage = 35;
            }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HollowWarriorBanner>();

            NPC.buffImmune[BuffID.Confused] = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.CanPassThroughWalls = true;
            globalNPC.HasGhostAfterimages = true;
            // Step 6 ghost levers: drift (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            // Movement: shared fighter mover + SF4 A* nav (Phase-3 migration off the bespoke jump-ladder).
            // (Ground nav; its wall-phase escape via CanPassThroughWalls still works independently.)
            globalNPC.HealthScaledSpeedBase = 2f;
            globalNPC.HealthScaledSpeedMultiplier = -0.75f; // full-health topSpeed 1.25 — was 0.75, sluggish under SF4 nav; speeds toward 2.0 when wounded (compensates SF4 overhead)
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 9f;            // default reach; SF4's up-and-over arc needs this to climb out of ~6-tile pits (TibianAmazon escapes the same pit on 9)
            globalNPC.RemembersLastKnownPos = true; // pursuer: investigate last-seen spot before drifting off
            // Poise / stagger: opt in. A stagger cancels a windup attack via IStaggerable.OnStagger below.
            globalNPC.PoiseMax = 15f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Staggered (poise broken): frozen. GlobalNPC.ApplyStaggerMovement drives the knockback slide in
            // PostAI; OnStagger already cancelled any in-progress attack. Freeze here so attack timers don't advance.
            if (globalNPC.StaggerTimer > 0)
            {
                globalNPC.AttackTelegraphing = false;
                globalNPC.AttackCommitted = false;
                return;
            }

            bool grounded = NPC.velocity.Y == 0; // proxy for the old standing-on-solid-tile scan (gates attacks)
            bool los = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            // The player is a same-level shield/melee threat only when roughly level with us AND visible. When
            // they're on another level (e.g. above on a ledge), shielding would just pin us in place and stop SF4
            // from pathing toward them — so the shield + jump-slash telegraph below are gated on this.
            bool playerMeleeLevel = los && Math.Abs(player.Center.Y - NPC.Center.Y) <= 4 * 16;

            // Movement is now the shared fighter mover (SF4 A* nav via the SetDefaults levers). This REPLACES the
            // deleted bespoke accel/brake + -5/-6/-7/-8 jump-ladder + platform-drop + boredom block (Phase-3
            // migration). Gated OUT during the melee lunges (slash / jump-slash) so they fully own velocity AND
            // facing. It STILL runs while shielding so the block tracks/faces the player (the shield code zeroes
            // velocity afterward). The ghost's wall-phase escape (CanPassThroughWalls) is independent of this.
            if (!slashing && !jumpSlashing)
            {
                tsorcRevampAIs.FighterAI(NPC, 2f, 0.08f, 0.1f, canPounce: false, canDodgeroll: false);
            }

            // Restore the SetDefaults knockBackResist each tick (0 = ghost is knockback-immune); poise owns the
            // stagger (a poise break still launches it via ApplyStaggerImpulse) and hyper-armor (AttackCommitted).
            if (globalNPC.BaseKnockBackResist >= 0f)
            {
                NPC.knockBackResist = globalNPC.BaseKnockBackResist;
            }

            // Poise labels (RedKnight / BlackKnight pattern). WINDUP (poise CAN break it, evasive reaction
            // suppressed) = the swing's wind-in. COMMITTED = hyper-armor (uninterruptible except by a stagger) =
            // the active swing. Basic slash: ai[3] windup <26, active 26-35. Jump-slash: ai[1] windup <436, active
            // lunge→hit 436-446. (Recovery after each is intentionally vulnerable — punishable, Souls-style.)
            globalNPC.AttackTelegraphing = (slashing && NPC.ai[3] < 26);
            // Jump-slash is hyper-armored for its whole committed wind-up→hit (red flash fires on commit at
            // ai[1]==420, slash connects at 442) — only a stagger can stop it. Recovery (>446) is vulnerable.
            globalNPC.AttackCommitted = (slashing && NPC.ai[3] >= 26 && NPC.ai[3] <= 35)
                                        || (jumpSlashing && NPC.ai[1] <= 455);

            #region overhead air-slash (hop straight up to a player directly above)
            // Kept as an ATTACK trigger: when the player is right above and in reach, hop up and slash.
            if (grounded && !slashing && !shielding && !jumpSlashing
                && NPC.position.Y > player.position.Y + 3 * 16
                && Math.Abs(NPC.Center.X - player.Center.X) < 4f * 16
                && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
            {
                slashing = true;
                NPC.ai[3] = 16;
                NPC.velocity.Y = -8f; // hop up to slash the overhead player
                NPC.netUpdate = true;
            }
            #endregion

            #region attacks


            //Basic Slash Attack

            if (NPC.ai[3] < 10)
            {
                ++NPC.ai[3]; //Used for Basic Slash
            }

            if (/*!shielding && */!jumpSlashing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) < 45 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    shielding = false;
                }

                if (slashing)
                {
                    ++NPC.ai[3];

                    if (NPC.ai[3] < 26)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.25f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.25f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }

                    if (NPC.ai[3] == 26) //If timer is 46
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center); //Play slash/swing sound

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                if (!grounded)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -56), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), smallSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);

                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), smallSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                                }
                            }

                            else
                            {
                                if (!grounded)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -56), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), smallSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);

                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), smallSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                                }
                            }
                        }                         
                    }

                    if (NPC.ai[3] >= 49) //If timer is 69
                    {
                        slashing = false;
                        NPC.ai[3] = 0; //Reset timer
                    }
                }
            }




            //Telegraphed Jump-slash

            if (NPC.ai[1] < 420)
            {
                ++NPC.ai[1]; //Used for Jump-slash
            }
            if (NPC.ai[1] >= 390 && NPC.ai[1] <= 400 && playerMeleeLevel && NPC.Distance(player.Center) < 150)
            {
                if (NPC.direction == 1) //Large eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

            }
            if (NPC.ai[1] >= 400 && NPC.ai[1] < 442 && playerMeleeLevel && NPC.Distance(player.Center) < 150)
            {
                if (NPC.direction == 1) //Small eye dust to warn player that a jump-slash is ready...
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }

                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y + 1), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    //dust2.velocity *= 0f;
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }

            if (/*!shielding && */!slashing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 120 && NPC.Distance(player.Center) >= 45 && NPC.velocity.Y == 0 && grounded && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0)) //If timer is at 0 and player is within slash range
                {
                    jumpSlashing = true;
                    shielding = false;

                    // Red telegraph flash on commit (the wind-up→hit below is hyper-armored, so only a stagger stops it).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Red));
                    }
                }

                if (jumpSlashing)
                {
                    ++NPC.ai[1];
                    if (NPC.ai[1] < 445) // extended wind-up: 25 ticks of hyper-armor after the red commit-flash (420) before the leap
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.15f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.15f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }

                    if (NPC.ai[1] == 445) //leap (25 ticks after the commit-flash at 420)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X += 5f;
                            NPC.velocity.Y -= 3f;
                        }

                        else
                        {
                            NPC.velocity.X -= 5f;
                            NPC.velocity.Y -= 3f;
                        }
                    }

                    if (NPC.ai[1] == 451) //slash hit
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center); //Play slash/swing sound

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), bigSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                            }

                            else
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-20, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.SmallWeaponSlash>(), bigSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                        }
                    }
                    if (NPC.ai[1] > 470 && NPC.ai[1] < 530)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.3f;
                            if (NPC.velocity.X < 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }

                        else
                        {
                            NPC.velocity.X += 0.3f;
                            if (NPC.velocity.X > 0)
                            {
                                NPC.velocity.X = 0;
                            }
                        }
                    }
                    if (NPC.ai[1] >= 530) //If timer is 530
                    {
                        jumpSlashing = false;
                        NPC.ai[1] = 0; //Reset timer
                    }
                }
            }




            //Shielding

            if (playerMeleeLevel && (shielding || NPC.Distance(player.Center) < 220 || NPC.ai[2] > 300))
            {
                NPC.ai[2]++;

                if (!jumpSlashing && !slashing && NPC.velocity.Y == 0)
                {
                    if (NPC.ai[2] > 300 && NPC.ai[2] <= 310)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; }
                        else { NPC.velocity.X += 0.15f; }
                    }

                    if (NPC.ai[2] > 310)
                    {
                        NPC.velocity.X = 0;
                        shielding = true;
                    }

                    if (NPC.ai[2] > 500)
                    {
                        shielding = false;
                        NPC.ai[2] = 0;
                    }
                }
            }
            else if (!playerMeleeLevel)
            {
                // Player on another level → don't shield-pin; pursue via SF4 and bleed the timer so a stale value
                // doesn't re-pin us the instant we reach their level.
                shielding = false;
                if (NPC.ai[2] > 0) NPC.ai[2] -= 2;
            }

            // Block stance this frame: a FRONT hit takes reduced poise (see GlobalNPC.ShieldGuarding + the doubled
            // front damage reduction in ModifyHitBy). Backstabs are unaffected.
            globalNPC.ShieldGuarding = shielding;
            #endregion
        }

        // IStaggerable: a poise break cancels any in-progress attack and returns to neutral approach.
        public void OnStagger(NPC npc)
        {
            slashing = false;
            jumpSlashing = false;
            shielding = false;
            npc.ai[1] = 60f;   // jump-slash timer → neutral (well below the 420 trigger)
            npc.ai[2] = -100f; // shield timer → delayed
            npc.ai[3] = 0f;    // basic-slash timer → neutral
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; //Flip texture depending on spriteDirection

            Texture2D texture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowWarrior");
            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), Color.White * 0.75f, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 64, 54), Color.White * 0.75f, NPC.rotation, new Vector2(32, 30), NPC.scale, effects, 0f);
            }
            return false; //Don't draw base sprite
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowWarrior_Shield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 10, 0, shieldFrame);
            if (shielding && NPC.velocity.X == 0 && !jumpSlashing && !slashing)
            {
                if (NPC.spriteDirection == 1)
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, Color.White * 0.2f, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, Color.White * 0.2f, NPC.rotation, new Vector2(32, 29), NPC.scale, effects, 0f);
                }
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (shielding && !slashing && !jumpSlashing)
            {
                if (NPC.ai[1] < 370)
                {
                    NPC.ai[1] += 30; //Used for Jump-slash
                }

                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 30;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.position); //Play dig
                        modifiers.SourceDamage.Flat -= 30;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 20;
                        }
                    }
                }
            }

            if (NPC.direction == 1) //if enemy facing right
            {
                if (player.position.X < NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weak spot!", false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }

            NPC.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[NPC.target];

            int direction = modifiers.HitDirection;

            Type hm = typeof(NPC.HitModifiers);
            PropertyInfo prop = hm.GetProperty("HitDirectionOverride");
            int? over = (int?)prop.GetValue(modifiers);

            if (over != null && over != 0)
            {
                direction = over.Value;
            }


            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.Specialist.BlizzardBlasterShot>())
            {
                if (shielding && !slashing && !jumpSlashing)
                {

                    if (NPC.direction == 1) //if npc facing right
                    {
                        if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }

                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.1f;

                            if (NPC.ai[1] < 380)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;
                            modifiers.Knockback *= 0.2f;

                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 30; //Used for Jump-slash
                            }

                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, NPC.Center); //Play dig sound
                            modifiers.SourceDamage.Flat -= 30;

                            modifiers.Knockback *= 0.1f;
                            if (NPC.ai[1] < 370)
                            {
                                NPC.ai[1] += 40; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 20;
                            }
                        }
                    }
                }


                if (NPC.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (direction == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }
                else //if enemy facing left
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.aiStyle != 19) //if hit in the back
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                    else if (direction == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2; //bonus damage
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                    }
                }

                if (NPC.Distance(player.Center) > 220 && !shielding)
                {
                    NPC.ai[2] += 100;
                }

                if (NPC.ai[1] < 400)
                {
                    NPC.ai[1] += 10;
                }
            }
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion || Sky(p) || spawnInfo.Player.ZoneSnow)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (spawnInfo.Water || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (spawnInfo.Player.ZoneGraveyard && !Main.hardMode && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 185 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 215
                 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 301 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 214 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 302)) chance = 3f;

            return chance;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 15));

        }

        #region Drawing and Animation

        public override void FindFrame(int frameHeight)
        {

            if (NPC.velocity.X != 0) //Walking
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12)
                {
                    NPC.frame.Y = 2 * frameHeight;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = 3 * frameHeight;
                }
                else if (NPC.frameCounter < 36)
                {
                    NPC.frame.Y = 4 * frameHeight;
                }
                else if (NPC.frameCounter < 48)
                {
                    NPC.frame.Y = 5 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = 6 * frameHeight;
                }
                else if (NPC.frameCounter < 72)
                {
                    NPC.frame.Y = 7 * frameHeight;
                }
                else if (NPC.frameCounter < 84)
                {
                    NPC.frame.Y = 8 * frameHeight;
                }
                else if (NPC.frameCounter < 96)
                {
                    NPC.frame.Y = 9 * frameHeight;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            if (NPC.velocity.Y != 0 && (!jumpSlashing || !shielding)) //If falling/jumping
            {
                NPC.frame.Y = 1 * frameHeight;
            }

            if (slashing) //If slashing
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[3] < 18)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (NPC.ai[3] < 26)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.ai[3] < 29)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.ai[3] < 32)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.ai[3] < 35)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                else if (NPC.ai[3] < 49)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }
            if (jumpSlashing) //If jumpslashing
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 437)
                {
                    NPC.frame.Y = 11 * frameHeight;
                }
                else if (NPC.ai[1] < 445)
                {
                    NPC.frame.Y = 12 * frameHeight;
                }
                else if (NPC.ai[1] < 448)
                {
                    NPC.frame.Y = 13 * frameHeight;
                }
                else if (NPC.ai[1] < 451)
                {
                    NPC.frame.Y = 14 * frameHeight;
                }
                else if (NPC.ai[1] < 454)
                {
                    NPC.frame.Y = 15 * frameHeight;
                }
                else if (NPC.ai[1] < 530)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing) //If not moving at all (shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding)
            {
                shieldAnimTimer++;

                if (shieldAnimTimer < 4)
                {
                    shieldFrame = 0 * 1;
                }
                else if (shieldAnimTimer < 8)
                {
                    shieldFrame = 1 * 1;
                }
                else if (shieldAnimTimer < 12)
                {
                    shieldFrame = 2 * 1;
                }
                else if (shieldAnimTimer < 16)
                {
                    shieldFrame = 3 * 1;
                }
                else if (shieldAnimTimer < 20)
                {
                    shieldFrame = 4 * 1;
                }
                else if (shieldAnimTimer < 24)
                {
                    shieldFrame = 5 * 1;
                }
                else if (shieldAnimTimer < 28)
                {
                    shieldFrame = 6 * 1;
                }
                else if (shieldAnimTimer < 32)
                {
                    shieldFrame = 7 * 1;
                }
                else if (shieldAnimTimer < 36)
                {
                    shieldFrame = 8 * 1;
                }
                else if (shieldAnimTimer < 40)
                {
                    shieldFrame = 9 * 1;
                }
                else if (shieldAnimTimer < 44)
                {
                    shieldFrame = 8 * 1;
                }
                else if (shieldAnimTimer < 48)
                {
                    shieldFrame = 7 * 1;
                }
                else if (shieldAnimTimer < 52)
                {
                    shieldFrame = 6 * 1;
                }
                else if (shieldAnimTimer < 56)
                {
                    shieldFrame = 5 * 1;
                }
                else if (shieldAnimTimer < 60)
                {
                    shieldFrame = 4 * 1;
                }
                else if (shieldAnimTimer < 64)
                {
                    shieldFrame = 3 * 1;
                }
                else if (shieldAnimTimer < 68)
                {
                    shieldFrame = 2 * 1;
                }
                else if (shieldAnimTimer < 72)
                {
                    shieldFrame = 1 * 1;
                }
                else
                {
                    shieldAnimTimer = 0;
                }
            }
        }

        #endregion

    }
}
