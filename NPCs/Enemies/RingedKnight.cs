using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class RingedKnight : ModNPC, IStaggerable
    {
        //AI
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;
        bool stabbing = false;
        bool enrage = false;
        bool hasEnraged = false;
        int enrageTimer;

        public int ringedKnightDamage = 14;
        public int fireDamage = 18;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 19;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            NPC.damage = 32;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 400;
            NPC.value = 2000;
            if (Main.hardMode)
            {
                NPC.lifeMax = 800;
                NPC.defense = 45;
                NPC.damage = 82;
                NPC.value = 4000;
                ringedKnightDamage = 25;
                fireDamage = 28;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2500;
                NPC.defense = 100;
                NPC.damage = 130;
                NPC.value = 10000;
                NPC.knockBackResist = 0.0f;
                ringedKnightDamage = 35;
                fireDamage = 38;
            }
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.RingedKnightBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            // Movement: shared fighter mover + SF4 A* nav. Replaces the deleted bespoke jump-ladder + boredom/lava
            // teleport. This is the slow, zoning fire knight, so topSpeed is computed in AI (1.5 → 2.0 enraged).
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 9.5f;
            globalNPC.RemembersLastKnownPos = true;
            // Unified teleport (replaces the hand-rolled boredom/lava blink) — fire-themed VFX to match the theme.
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;
            // Poise (a stagger cancels the wind-up) — knights configure in-file, not in PopulatePoiseProfiles.
            globalNPC.PoiseMax = 30f;
            // Evasive on-hit: shared knight-family reactions (hop/dash away, blink when able).
            EvasiveProfile.RedKnight(globalNPC);
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

            if (globalNPC.StaggerTimer > 0)
            {
                globalNPC.AttackTelegraphing = false;
                globalNPC.AttackCommitted = false;
                return;
            }

            Lighting.AddLight(NPC.Center, .28f, .16f, .04f);

            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            var projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>();
            var projStab = ModContent.ProjectileType<Projectiles.Enemy.Spearhead>();

            #region enrage (≤60% HP: fire projectiles + one-time +25% damage + ambient flame)
            if (lifePercentage <= 60)
            {
                projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlashFire>();
                projStab = ModContent.ProjectileType<Projectiles.Enemy.SpearheadFire>();
                if (!hasEnraged)
                    enrage = true;
            }

            if (enrage)
            {
                enrageTimer++;

                if (enrageTimer <= 30)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2.5f));
                        Main.dust[dust].noGravity = true;
                    }
                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }

                if (enrageTimer >= 90)
                {
                    hasEnraged = true;
                    enrage = false;
                    ringedKnightDamage += (int)(ringedKnightDamage * 0.25f); // one-time enrage damage boost
                }
            }

            if (!stabbing && !slashing && !jumpSlashing && lifePercentage <= 60 && Main.rand.NextBool(4))
            {
                if (NPC.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X + 26, NPC.position.Y - 4), 34, 34, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }
                else
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X - 40, NPC.position.Y - 4), 34, 34, 6, 0, 0, 30, default(Color), Main.rand.NextFloat(1.2f, 2f));
                    Main.dust[dust].noGravity = true;
                }
            }
            #endregion

            bool grounded = NPC.velocity.Y == 0;
            bool los = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            bool playerMeleeLevel = los && Math.Abs(player.Center.Y - NPC.Center.Y) <= 4 * 16;

            // Shared fighter mover + SF4 nav (with Fire teleport + lava hopping for the Underworld). Gated OUT during
            // the melee attacks so they own velocity/facing.
            float topSpeed = lifePercentage <= 60 ? 2f : 1.5f;
            if (!slashing && !jumpSlashing && !stabbing)
                tsorcRevampAIs.FighterAI(NPC, topSpeed, 0.05f, 0.15f, canTeleport: true, lavaJumping: true, canPounce: false, canDodgeroll: false);

            if (globalNPC.BaseKnockBackResist >= 0f)
                NPC.knockBackResist = globalNPC.BaseKnockBackResist;

            // Poise labels. Basic slash: interruptible windup (<26), hyper-armor active (26-35). Jump-slash & dash-stab
            // hyper-armored from their commit flash. Flamethrower hyper-armored while channeling (355 flash → 460).
            globalNPC.AttackTelegraphing = (slashing && NPC.ai[3] < 26);
            globalNPC.AttackCommitted = (slashing && NPC.ai[3] >= 26 && NPC.ai[3] <= 35)
                || (jumpSlashing && NPC.ai[1] <= 455)
                || stabbing
                || (shielding && NPC.ai[2] >= 355 && NPC.ai[2] <= 460);

            // overhead air-slash when standing below player
            if (grounded && !slashing && !shielding && !jumpSlashing && !stabbing
                && NPC.position.Y > player.position.Y + 3 * 16
                && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && los)
            {
                slashing = true;
                NPC.ai[3] = NPC.position.Y < player.position.Y + 8 * 16 ? 22 : 10;
                NPC.velocity.Y = NPC.position.Y < player.position.Y + 8 * 16 ? -8f : -9.5f;
                NPC.netUpdate = true;
            }

            #region attacks

            // Basic Slash (ai[3])
            if (NPC.ai[3] < 10)
                ++NPC.ai[3];

            if (!jumpSlashing && !stabbing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) <= 55 && los)
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
                            if (NPC.velocity.X < 0) NPC.velocity.X = 0;
                        }
                        else
                        {
                            NPC.velocity.X += 0.25f;
                            if (NPC.velocity.X > 0) NPC.velocity.X = 0;
                        }
                    }

                    if (NPC.ai[3] == 26)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                if (!grounded)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -66), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                            else
                            {
                                if (!grounded)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -66), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -20), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                        }
                    }

                    if (NPC.ai[3] >= 49)
                    {
                        slashing = false;
                        NPC.ai[3] = 0;
                    }
                }
            }

            // Telegraphed Jump-slash (ai[1]) — red flash at 420, 25-tick wind-up, leap 445, hit 451
            if (NPC.ai[1] < 420)
                ++NPC.ai[1];

            // Eye-dust "primed" tell: the 60 ticks before the attack can commit (ai[1] reaches 420).
            if (NPC.ai[1] >= 360 && NPC.ai[1] < 390)
            {
                Vector2 dustPos = NPC.direction == 1 ? new Vector2(NPC.position.X + 9, NPC.position.Y) : new Vector2(NPC.position.X + 3, NPC.position.Y);
                Dust dust2 = Main.dust[Dust.NewDust(dustPos, 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                dust2.noGravity = true;
                dust2.fadeIn = .3f;
                dust2.velocity += NPC.velocity;
            }

            if (NPC.ai[1] >= 390 && NPC.ai[1] < 420)
            {
                Vector2 dustPos = NPC.direction == 1 ? new Vector2(NPC.position.X + 9, NPC.position.Y) : new Vector2(NPC.position.X + 3, NPC.position.Y);
                Dust dust2 = Main.dust[Dust.NewDust(dustPos, 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                dust2.noGravity = true;
                dust2.fadeIn = .3f;
                dust2.velocity += NPC.velocity;
            }

            if (!slashing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 150 && NPC.Distance(player.Center) >= 55 && grounded && los)
                {
                    jumpSlashing = true;
                    shielding = false;
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Red));
                }

                if (jumpSlashing)
                {
                    ++NPC.ai[1];

                    if (NPC.ai[1] < 445) // 25-tick hyper-armored wind-up after the red commit-flash (420) before the leap
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.15f;
                            if (NPC.velocity.X < 0) NPC.velocity.X = 0;
                        }
                        else
                        {
                            NPC.velocity.X += 0.15f;
                            if (NPC.velocity.X > 0) NPC.velocity.X = 0;
                        }
                    }

                    if (NPC.ai[1] == 445) // leap
                    {
                        if (NPC.direction == 1) { NPC.velocity.X += 5f; NPC.velocity.Y -= 3f; }
                        else { NPC.velocity.X -= 5f; NPC.velocity.Y -= 3f; }
                    }

                    if (NPC.ai[1] == 451) // slash hit
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(24, -20), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            else
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), projSlash, (int)(ringedKnightDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                        }
                    }

                    if (NPC.ai[1] > 470 && NPC.ai[1] < 489)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.3f;
                            if (NPC.velocity.X < 0) NPC.velocity.X = 0;
                        }
                        else
                        {
                            NPC.velocity.X += 0.3f;
                            if (NPC.velocity.X > 0) NPC.velocity.X = 0;
                        }
                    }

                    if (NPC.ai[1] >= 489)
                    {
                        jumpSlashing = false;
                        NPC.ai[1] = 150;
                    }
                }
            }

            // Dash-stab COMBO finisher: chains off the end of a basic slash (ai[3] >= 48) or the jump-slash recovery
            // (ai[1] == 488). Fast (~6t) — it's earned by landing the prior attack — with a brief yellow tell.
            if (!stabbing && NPC.Distance(player.Center) < 160 && NPC.Distance(player.Center) >= 55 && grounded
                && Math.Abs(NPC.Center.Y - player.Center.Y) < 4.5f * 16 && los
                && (NPC.ai[3] >= 48 || (NPC.ai[1] == 488 && jumpSlashing)))
            {
                NPC.TargetClosest(true);
                stabbing = true;
                shielding = false;
                jumpSlashing = false;
                NPC.ai[1] = 430;
                slashing = false;
                NPC.ai[3] = 0;
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Yellow));
            }

            if (stabbing)
            {
                ++NPC.ai[1];

                if (NPC.ai[1] < 436)
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X -= 0.15f;
                        if (NPC.velocity.X < 0) NPC.velocity.X = 0;
                    }
                    else
                    {
                        NPC.velocity.X += 0.15f;
                        if (NPC.velocity.X > 0) NPC.velocity.X = 0;
                    }
                }

                if (NPC.ai[1] == 436)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 1.0f, PitchVariance = 0.3f }, player.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (lifePercentage <= 60)
                        {
                            if (NPC.direction == 1)
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(48, -2), new Vector2(0, 0), projStab, (int)(ringedKnightDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X += 10.5f;
                            }
                            else
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-48, -2), new Vector2(0, 0), projStab, (int)(ringedKnightDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X -= 10.5f;
                            }
                        }
                        else
                        {
                            if (NPC.direction == 1)
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), projStab, (int)(ringedKnightDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X += 10.5f;
                            }
                            else
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-44, -2), new Vector2(0, 0), projStab, (int)(ringedKnightDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X -= 10.5f;
                            }
                        }
                    }
                }

                if (NPC.ai[1] > 450 && NPC.ai[1] < 520)
                {
                    if (NPC.direction == 1)
                    {
                        NPC.velocity.X -= 0.3f;
                        if (NPC.velocity.X < 0) NPC.velocity.X = 0;
                    }
                    else
                    {
                        NPC.velocity.X += 0.3f;
                        if (NPC.velocity.X > 0) NPC.velocity.X = 0;
                    }
                }

                if (NPC.ai[1] > 520)
                {
                    NPC.ai[1] = 280;
                    stabbing = false;
                }
            }

            // Shielding → Flamethrower breath (ai[2]). Gated on the player being on our level (don't shield-pin when
            // they're above/below — pursue via SF4 instead). Orange commit-flash at 355, 25 ticks before the breath.
            if (playerMeleeLevel)
            {
                NPC.ai[2]++;

                if (!jumpSlashing && !slashing && !stabbing && NPC.velocity.Y == 0)
                {
                    if (NPC.ai[2] > 300 && NPC.ai[2] <= 310)
                    {
                        if (NPC.direction == 1) NPC.velocity.X -= 0.15f;
                        else NPC.velocity.X += 0.15f;
                    }

                    if (NPC.ai[2] > 310)
                    {
                        NPC.velocity.X = 0;
                        shielding = true;
                    }

                    if (NPC.ai[2] == 355 && NPC.Distance(player.Center) > 55 && NPC.Distance(player.Center) < 300)
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Orange));

                    if (NPC.ai[2] > 380 && NPC.ai[2] < 460 && NPC.Distance(player.Center) > 55 && NPC.Distance(player.Center) < 300)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (20f * NPC.direction), NPC.Center.Y, 8f * NPC.direction, Main.rand.NextFloat(-1.5f, 0f), ModContent.ProjectileType<Projectiles.Enemy.SmallFlameJet>(), fireDamage, 0f, Main.myPlayer);
                            Main.projectile[num54].timeLeft = 25;
                            if (Main.rand.NextBool(3))
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, PitchVariance = 0.1f }, NPC.Center);
                        }
                    }

                    if (NPC.ai[2] > 530)
                    {
                        shielding = false;
                        NPC.ai[2] = 0;
                    }
                }
            }
            else
            {
                shielding = false;
                if (NPC.ai[2] > 0) NPC.ai[2] -= 2;
            }

            globalNPC.ShieldGuarding = shielding;
            #endregion
        }

        public void OnStagger(NPC npc)
        {
            slashing = false;
            jumpSlashing = false;
            shielding = false;
            stabbing = false;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.ai[3] = 0f;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            int shieldPower = NPC.defense * 2;

            if (shielding)
            {
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                    }
                }
            }

            if (NPC.direction == 1)
            {
                if (player.position.X < NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                }
            }
            else
            {
                if (player.position.X > NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
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
                direction = over.Value;

            int shieldPower = NPC.defense * 3;

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.Specialist.BlizzardBlasterShot>())
            {
                if (shielding)
                {
                    if (NPC.direction == 1)
                    {
                        if (projectile.Center.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 70;
                            if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                        }
                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 80;
                            if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                        }
                    }
                    else
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 70;
                            if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 80;
                            if (NPC.ai[2] > 355) NPC.ai[2] -= 25;
                        }
                    }
                }

                if (NPC.direction == 1)
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }
                else
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }

                if (NPC.Distance(player.Center) > 220 && !shielding)
                    NPC.ai[2] += 120;

                if (NPC.ai[1] < 400)
                    NPC.ai[1] += 10;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.townNPCs > 1f) return 0f;

            if (spawnInfo.Player.ZoneUnderworldHeight) return 0.1f;

            if (spawnInfo.Player.ZoneUnderworldHeight && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.LavaMossBlockWall || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.LavaUnsafe2 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.LavaUnsafe1)) return chance = 0.15f;

            if (Main.hardMode && spawnInfo.Player.ZoneUndergroundDesert) return 0.1f; // now spawns in desert HM

            return chance;
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 6));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Humanity>(), 12));
            npcLoot.Add(new CommonDrop(ItemID.RagePotion, 100, 1, 1, 10));
            npcLoot.Add(new CommonDrop(ItemID.WrathPotion, 100, 1, 1, 10));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<CrimsonPotion>(), 100, 1, 1, 10));
        }

        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor)
        {
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if ((NPC.velocity.X > 5f || NPC.velocity.X < -5f) && stabbing)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, NPC.gfxOffY);
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(NPC.position.X + 26, NPC.position.Y + 12), NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D texture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/RingedKnight_Glow");
            Texture2D firesword = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/RingedKnight_FireSword");
            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);

            if (lifePercentage <= 60)
            {
                spriteBatch.Draw(firesword, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 94, 58), Color.White, NPC.rotation, new Vector2(47, 34), NPC.scale, effects, 0f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.X != 0)
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12) NPC.frame.Y = 2 * frameHeight;
                else if (NPC.frameCounter < 24) NPC.frame.Y = 3 * frameHeight;
                else if (NPC.frameCounter < 36) NPC.frame.Y = 4 * frameHeight;
                else if (NPC.frameCounter < 48) NPC.frame.Y = 5 * frameHeight;
                else if (NPC.frameCounter < 60) NPC.frame.Y = 6 * frameHeight;
                else if (NPC.frameCounter < 72) NPC.frame.Y = 7 * frameHeight;
                else if (NPC.frameCounter < 84) NPC.frame.Y = 8 * frameHeight;
                else if (NPC.frameCounter < 96) NPC.frame.Y = 9 * frameHeight;
                else NPC.frameCounter = 0;
            }

            if (NPC.velocity.Y != 0 && (!jumpSlashing && !shielding && !stabbing))
                NPC.frame.Y = 1 * frameHeight;

            if (slashing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[3] < 18) NPC.frame.Y = 12 * frameHeight;
                else if (NPC.ai[3] < 26) NPC.frame.Y = 13 * frameHeight;
                else if (NPC.ai[3] < 29) NPC.frame.Y = 14 * frameHeight;
                else if (NPC.ai[3] < 32) NPC.frame.Y = 15 * frameHeight;
                else if (NPC.ai[3] < 35) NPC.frame.Y = 16 * frameHeight;
                else if (NPC.ai[3] < 49) NPC.frame.Y = 17 * frameHeight;
            }

            if (jumpSlashing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 437) NPC.frame.Y = 12 * frameHeight;
                else if (NPC.ai[1] < 445) NPC.frame.Y = 13 * frameHeight;
                else if (NPC.ai[1] < 448) NPC.frame.Y = 14 * frameHeight;
                else if (NPC.ai[1] < 451) NPC.frame.Y = 15 * frameHeight;
                else if (NPC.ai[1] < 454) NPC.frame.Y = 16 * frameHeight;
                else if (NPC.ai[1] < 489) NPC.frame.Y = 17 * frameHeight;
            }

            if (stabbing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 436) NPC.frame.Y = 2 * frameHeight;
                else if (NPC.ai[1] < 470) NPC.frame.Y = 18 * frameHeight;
                else if (NPC.ai[1] < 475) NPC.frame.Y = 16 * frameHeight;
                else if (NPC.ai[1] < 520) NPC.frame.Y = 17 * frameHeight;
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing && !stabbing)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding && NPC.ai[2] > 360 && NPC.ai[2] < 460 && NPC.Distance(Main.player[NPC.target].Center) > 100 && NPC.Distance(Main.player[NPC.target].Center) < 300)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 11 * frameHeight;
            }
        }

        #endregion
    }
}
