using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Utilities;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    public class HollowSoldier : ModNPC, IStaggerable //Don't look at the code, it's muy malo. Look at Lothric Spear Knight for a better example code management-wise
    {
        //AI 
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;


        //Anim
        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public int hollowLesserSlashDamage = 17;
        public int hollowGreaterSlashDamage = 20;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 17;
        }
        public override void SetDefaults()
        {
            NPC.knockBackResist = 0.2f;
            NPC.aiStyle = -1;
            NPC.damage = 32;
            NPC.defense = 20;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 250;
            if (Main.hardMode)
            {
                NPC.lifeMax = 500;
                NPC.defense = 30;
                NPC.damage = 42;
                NPC.value = 2500; // was 150
                hollowLesserSlashDamage = 26;
                hollowGreaterSlashDamage = 30; // scaling damage added
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1500;
                NPC.defense = 70;
                NPC.damage = 58;
                NPC.value = 6000; //was 250, now has scaling damage
                hollowLesserSlashDamage = 30;
                hollowGreaterSlashDamage = 34;
            }
            NPC.value = 1250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.HollowSoldierBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            // Movement: shared fighter mover + SF4 A* nav (Phase-3 migration off the bespoke jump-ladder).
            globalNPC.HealthScaledSpeedBase = 2f;
            globalNPC.HealthScaledSpeedMultiplier = -0.75f; // full-health topSpeed 1.25 - was 0.75, sluggish under SF4 nav; speeds toward 2.0 when wounded (compensates SF4 overhead)
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 9f;            // default reach; SF4's up-and-over arc needs this to climb out of ~6-tile pits (TibianAmazon escapes the same pit on 9)
            globalNPC.RemembersLastKnownPos = true; // melee pursuer: investigate last-seen spot before patrolling
            // Poise / stagger: opt in. A stagger cancels a windup attack via IStaggerable.OnStagger below.
            globalNPC.PoiseMax = 20f;               // sturdier than Hollow Warrior (more HP/armor)
            // Reactive shield: pre-emptive + on-hit block chance. See ShieldProfile.
            ShieldProfile.Hollow(globalNPC);
        }

        // On-hit reactive block: a chance to snap the guard up to catch the rest of a combo.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
            => tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), true);

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
            => tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), projectile.DamageType == DamageClass.Melee);

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
            // from pathing toward them - so the shield + jump-slash telegraph below are gated on this.
            bool playerMeleeLevel = los && Math.Abs(player.Center.Y - NPC.Center.Y) <= 4 * 16;

            // Movement is now the shared fighter mover (SF4 A* nav via the SetDefaults levers). This REPLACES the
            // deleted bespoke accel/brake + -5/-6/-7/-8 jump-ladder + platform-drop + boredom block (Phase-3
            // migration). Gated OUT during the melee lunges (slash / jump-slash) so they fully own velocity AND
            // facing. It STILL runs while shielding so the block tracks/faces the player (the shield code zeroes
            // velocity afterward). Mirrors FirebombHollow / BlackKnight: attacks below override the mover.
            if (!slashing && !jumpSlashing)
            {
                tsorcRevampAIs.FighterAI(NPC, 2f, 0.08f, 0.1f, canPounce: false, canDodgeroll: false);
            }

            // Restore the SetDefaults knockBackResist each tick; poise scales it to a light flinch and owns
            // hyper-armor (via AttackCommitted). Replaces the old scattered "knockback-immune while moving" hacks.
            if (globalNPC.BaseKnockBackResist >= 0f)
            {
                NPC.knockBackResist = globalNPC.BaseKnockBackResist;
            }

            // Poise labels (RedKnight / BlackKnight pattern). WINDUP (poise CAN break it, evasive reaction
            // suppressed) = the swing's wind-in. COMMITTED = hyper-armor (uninterruptible except by a stagger) =
            // the active swing. Basic slash: ai[3] windup <26, active 26-35. Jump-slash: ai[1] windup <436, active
            // lunge-hit 436-446. (Recovery after each is intentionally vulnerable - punishable, Souls-style.)
            globalNPC.AttackTelegraphing = (slashing && NPC.ai[3] < 26);
            // Jump-slash is hyper-armored for its whole committed wind-up-hit (red flash fires on commit at
            // ai[1]==420, slash connects at 442) - only a stagger can stop it. Recovery (>446) is vulnerable.
            globalNPC.AttackCommitted = (slashing && NPC.ai[3] >= 26 && NPC.ai[3] <= 35)
                                        || (jumpSlashing && NPC.ai[1] <= 455);

            // Pre-emptive block: in neutral, a chance to raise the guard when a threat (incoming shot / close player)
            // is detected - before the hit lands.
            if (!slashing && !jumpSlashing && !shielding && grounded)
                tsorcRevampAIs.TryPreemptiveBlock(NPC, globalNPC);

            #region overhead air-slash (hop straight up to a player directly above)
            // Kept as an ATTACK trigger: when the player is right above and in reach, hop up and slash.
            if (grounded && !slashing && !shielding && !jumpSlashing
                && NPC.position.Y > player.position.Y + 3 * 16
                && Math.Abs(NPC.Center.X - player.Center.X) < 4f * 16
                && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
            {
                slashing = true;
                NPC.ai[3] = 20;
                NPC.velocity.Y = -8f; // hop up to slash the overhead player
                NPC.netUpdate = true;
            }
            #endregion

            #region attacks


            //Basic Slash Attack
            //Main.NewText(npc.ai[1]);
            //Main.NewText(npc.ai[2]);
            //Main.NewText(npc.ai[3]);
            // Main.NewText(top_speed);
            //Main.NewText(Math.Abs(npc.velocity.X));

            if (NPC.ai[3] < 10)
            {
                ++NPC.ai[3]; //Used for Basic Slash
            }

            if (/*!shielding && */!jumpSlashing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) < 50 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
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
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(14, -60), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowLesserSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(14, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowLesserSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                                }
                            }

                            else
                            {
                                if (!grounded)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-10, -60), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowLesserSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                                }
                                else
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-10, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowLesserSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
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

            if (/*!shielding &&*/ !slashing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 140 && NPC.Distance(player.Center) >= 50 && NPC.velocity.Y == 0 && grounded && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0)) //If timer is at 0 and player is within slash range
                {
                    jumpSlashing = true;
                    shielding = false;

                    // Red telegraph flash on commit (the wind-up-hit below is hyper-armored, so only a stagger stops it).
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
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(24, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowGreaterSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                            }

                            else
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), hollowGreaterSlashDamage, 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                        }                         
                    }
                    if (NPC.ai[1] > 470 && NPC.ai[1] < 510)
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
                    if (NPC.ai[1] >= 510) //If timer is 69
                    {
                        jumpSlashing = false;
                        NPC.ai[1] = 0; //Reset timer
                    }
                }
            }




            //Shielding

            // Shielding is REACTIVE only: the pre-emptive + on-hit block (ReactiveBlockTimer); the old autonomous
            // ai[2] timer metronome is gone. While guarding, plant in place on the idle shield frame.
            shielding = globalNPC.ReactiveBlockTimer > 0 && !jumpSlashing && !slashing && NPC.velocity.Y == 0;
            if (shielding)
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                NPC.velocity.X = 0;
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
            npc.ai[1] = 60f;   // jump-slash timer - neutral (well below the 420 trigger)
            npc.ai[2] = -100f; // shield timer - delayed
            npc.ai[3] = 0f;    // basic-slash timer - neutral
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            //("NPCs/Enemies/HollowSoldier_Shield");
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowSoldier_Shield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 15, 0, shieldFrame);
            if (shielding && !jumpSlashing && !slashing)
            {
                Vector2 shieldWalkOffset = Vector2.Zero;
                int currentFrame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
                if (currentFrame >= 17 && currentFrame <= 24)
                {
                    int shieldWalkFrame = currentFrame - 17;
                    int shieldBobX = shieldWalkFrame switch
                    {
                        0 => -2,
                        1 => -1,
                        2 => 0,
                        3 => 1,
                        4 => 2,
                        5 => 1,
                        6 => 0,
                        _ => -1,
                    };
                    int shieldBobY = shieldWalkFrame == 2 || shieldWalkFrame == 6 ? 1 : 0;
                    shieldWalkOffset = new Vector2(shieldBobX * -NPC.spriteDirection, shieldBobY);
                }

                if (NPC.spriteDirection == 1)
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center + shieldWalkOffset - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(34, 27), NPC.scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(shieldTexture, NPC.Center + shieldWalkOffset - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(34, 27), NPC.scale, effects, 0f);
                }
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (shielding)
            {
                if (NPC.ai[1] < 370)
                {
                    NPC.ai[1] += 50; //Used for Jump-slash
                }
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                        modifiers.SourceDamage.Flat -= 60;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 25;
                        }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                        modifiers.SourceDamage.Flat -= 60;
                        if (NPC.ai[2] > 350)
                        {
                            NPC.ai[2] -= 25;
                        }
                    }
                }
            }

            if (NPC.direction == 1) //if enemy facing right
            {
                if (player.position.X < NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2; //bonus damage
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center); //Play fleshy sound
                }
            }
            else //if enemy facing left
            {
                if (player.position.X > NPC.position.X) //if hit in the back
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
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
                if (shielding)
                {
                    if (NPC.direction == 1) //if npc facing right
                    {
                        if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            modifiers.SourceDamage.Flat -= 60;
                            modifiers.Knockback *= 0.1f;
                            if (NPC.ai[1] < 350)
                            {
                                NPC.ai[1] += 50; //Used for Jump-slash
                            }
                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }

                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            modifiers.SourceDamage.Flat -= 60;
                            modifiers.Knockback *= 0f;

                            if (NPC.ai[1] < 350)
                            {
                                NPC.ai[1] += 60; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                    }
                    else //if npc facing left
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if proj moving toward npc front
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            modifiers.SourceDamage.Flat -= 60;
                            modifiers.Knockback *= 0.1f;
                            if (NPC.ai[1] < 350)
                            {
                                NPC.ai[1] += 50; //Used for Jump-slash
                            }
                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center); //Play metal tink sound
                            modifiers.SourceDamage.Flat -= 60;

                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 350)
                            {
                                NPC.ai[1] += 60; //Used for Jump-slash
                            }


                            if (NPC.ai[2] > 350)
                            {
                                NPC.ai[2] -= 25;
                            }
                        }
                    }
                }


                if (NPC.direction == 1) //if enemy facing right
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if hit in the back
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
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19) //if hit in the back
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
                    NPC.ai[2] += 120;
                }

                if (NPC.ai[1] < 340)
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
            if (spawnInfo.Water) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (spawnInfo.Player.townNPCs > 1f) return 0f;

            if (!Main.hardMode && spawnInfo.SpawnTileType == TileID.GreenDungeonBrick && !spawnInfo.Water) return 0.12f;
            if (!spawnInfo.Water && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonSlabUnsafe || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.GreenDungeonUnsafe) && !Main.hardMode && !tsorcRevampWorld.SuperHardMode) return 0.12f;
            if (Main.hardMode && spawnInfo.Lihzahrd) return 0.2f;
            if (Main.hardMode && p.ZoneNormalCaverns && !spawnInfo.Water) return 0.02f;
            if (Main.hardMode && p.ZoneDesert && p.ZoneOverworldHeight && !spawnInfo.Water) return 0.05f;
            if (Main.hardMode && p.ZoneUndergroundDesert && !spawnInfo.Water) return 0.07f;
            if (Main.hardMode && spawnInfo.SpawnTileType == TileID.BlueDungeonBrick && !spawnInfo.Water) return 0.18f;
            if (Main.hardMode && spawnInfo.SpawnTileType == TileID.TungstenBrick && !spawnInfo.Water) return 0.15f;

            if (tsorcRevampWorld.SuperHardMode && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneUnderworldHeight)) return 0.23f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneOverworldHeight && !(Ocean || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return 0.25f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDesert) return 0.13f;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneUnderworldHeight) return 0.16f; //.08% is 4.28%

            if (Main.expertMode && Main.bloodMoon && spawnInfo.Player.ZoneOverworldHeight && (NPC.downedBoss2 || NPC.downedBoss3)) return chance = 0.03f;

            if (Main.expertMode && Main.bloodMoon && (NPC.downedBoss2 || NPC.downedBoss3)) return chance = 0.03f;

            if ((NPC.downedBoss2 || NPC.downedBoss3) && spawnInfo.Player.ZoneOverworldHeight && Main.dayTime && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.035f;
            if ((NPC.downedBoss2 || NPC.downedBoss3) && spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.075f;

            if ((NPC.downedBoss2 || NPC.downedBoss3) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.dayTime && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.06f;
            if ((NPC.downedBoss2 || NPC.downedBoss3) && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && !Main.dayTime && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.08f;

            if (NPC.downedBoss2 || NPC.downedBoss3 && !(Ocean || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) return chance = 0.025f;

            return chance;

        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 3, 9));
            npcLoot.Add(ItemDropRule.Common(ItemID.EndurancePotion, 30));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FadingSoul>(), 10));
            //npcLoot.Add(ItemDropRule.Common(ItemID.CobaltShield, 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<IronShield>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.Lifegem>(), 15));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Potions.RadiantLifegem>(), 30));

            int[] armorIDs = new int[] {
                ModContent.ItemType<Items.Armors.Magic.RedClothHat>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothTunic>(),
                ModContent.ItemType<Items.Armors.Magic.RedClothPants>(),
            };
            npcLoot.Add(new DropMultiple(armorIDs, 30, 1, !NPC.downedBoss1));
        }

        #region Drawing and Animation


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (shielding && !jumpSlashing && !slashing && NPC.velocity.X != 0)
            {
                Texture2D shieldWalkTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/HollowSoldier_ShieldWalk");
                int currentFrame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 2;
                int shieldWalkFrame = Math.Clamp(currentFrame - 2, 0, 7);
                Rectangle frame = shieldWalkTexture.Frame(1, 8, 0, shieldWalkFrame);
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                spriteBatch.Draw(shieldWalkTexture, NPC.Center - Main.screenPosition, frame, drawColor, NPC.rotation, new Vector2(34, 27), NPC.scale, effects, 0f);
                return false;
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            //Main.NewText(shieldAnimTimer);
            //Main.NewText(shieldFrame);

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
                else if (NPC.ai[1] < 510)
                {
                    NPC.frame.Y = 16 * frameHeight;
                }
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing) //If not moving at all (shielding)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding && !jumpSlashing && NPC.ai[1] <= 420)
            {
                shieldFrame = shieldAnimTimer / 4; //Me smart, me figure out how to make loop AND simplify code at the same time!

                if (shieldFrame == 0)
                {
                    countingUP = true;
                }
                if (shieldFrame <= 14 && countingUP)
                {
                    shieldAnimTimer++;
                }
                if (shieldFrame == 14)
                {
                    countingUP = false;
                }
                if (shieldFrame >= 0 && !countingUP)
                {
                    shieldAnimTimer--;
                }
            }
        }

        #endregion

    }
}
