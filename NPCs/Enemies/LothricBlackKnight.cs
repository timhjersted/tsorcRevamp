using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class LothricBlackKnight : ModNPC, IStaggerable
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/LothricKnight";

        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;
        bool stabbing = false;
        bool enrage = false;
        bool hasEnraged = false;
        int enrageTimer;

        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public int lothricDamage = 17;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 18;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            NPC.damage = 55;
            NPC.defense = 15;
            NPC.height = 44;
            NPC.width = 20;
            NPC.lifeMax = 1000;
            NPC.value = 5000;
            if (Main.hardMode)
            {
                NPC.knockBackResist = 0.05f;
                NPC.lifeMax = 1500;
                NPC.defense = 40;
                NPC.value = 7500;
                lothricDamage = 25;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.knockBackResist = 0.0f;
                NPC.lifeMax = 5000;
                NPC.defense = 95;
                NPC.damage = 65;
                NPC.value = 13000;
                lothricDamage = 30;
            }
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.LothricBlackKnightBanner>();
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.HealthScaledSpeedBase = 2.5f;
            globalNPC.HealthScaledSpeedMultiplier = -2.0f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.MaxJumpPower = 9.5f;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.PoiseMax = 35f;
            // Evasive (neutral-game only — suppressed during its own attacks): aggressive gap-closers that flow into
            // a slash on arrival. See EvasiveProfile.LothricKnight.
            EvasiveProfile.LothricKnight(globalNPC);
            // Reactive shield: pre-emptive + on-hit block chance. See ShieldProfile.
            ShieldProfile.LothricKnight(globalNPC);
        }

        // On-hit: roll a reactive block first (snap the guard up to catch the combo); only evade if it didn't block.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!tsorcRevampAIs.TryOnHitBlock(NPC, g, true))
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            bool melee = projectile.DamageType == DamageClass.Melee;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!tsorcRevampAIs.TryOnHitBlock(NPC, g, melee))
                tsorcRevampAIs.EvasiveOnHit(NPC, melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            player.AddBuff(36, 3 * 60, false);
            player.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 10 * 60, false);
        }
        #endregion

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

            UsefulFunctions.DustRing(NPC.Center, 500, DustID.RedTorch, 5, 2f);
            if (NPC.Distance(player.Center) < 500)
            {
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2);
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
            }
            if (Main.hardMode && NPC.Distance(player.Center) < 500)
            {
                player.AddBuff(ModContent.BuffType<Crippled>(), 30, false);
                player.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30, false);
            }

            var projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>();
            var projStab = ModContent.ProjectileType<Projectiles.Enemy.Spearhead>();

            if (NPC.life < NPC.lifeMax / 2)
            {
                projSlash = ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlashCrimson>();
                projStab = ModContent.ProjectileType<Projectiles.Enemy.SpearheadCrimson>();
                if (!hasEnraged) enrage = true;
            }

            if (enrage)
            {
                enrageTimer++;
                if (enrageTimer <= 120)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 60, 0, 0, 30, default(Color), Main.rand.NextFloat(1f, 1.5f));
                        Main.dust[dust].noGravity = true;
                    }
                }
                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(NPC.position.X - 10, NPC.position.Y - 15), NPC.width + 20, NPC.height + 20, 60, 0, 0, 30, default(Color), Main.rand.NextFloat(.8f, 1.2f));
                    Main.dust[dust].noGravity = true;
                }
                if (enrageTimer > 180)
                {
                    hasEnraged = true;
                    enrage = false;
                }
            }

            bool grounded = NPC.velocity.Y == 0;
            bool los = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);

            if (!slashing && !jumpSlashing && !stabbing)
                tsorcRevampAIs.FighterAI(NPC, 2.5f, 0.08f, 0.1f, canPounce: false, canDodgeroll: false);

            if (globalNPC.BaseKnockBackResist >= 0f)
                NPC.knockBackResist = globalNPC.BaseKnockBackResist;

            // Basic slash: interruptible windup (<26), then hyper-armored active swing (26-35). Jump-slash and
            // dash-stab are hyper-armored for their ENTIRE committed window — the red/yellow flash at ai[1]==420
            // marks the moment they become uninterruptible (only a stagger stops them). Jump-slash recovery (>455)
            // is vulnerable/punishable; the dash-stab stays armored through its recovery (short, intentional).
            globalNPC.AttackTelegraphing = (slashing && NPC.ai[3] < 26);
            globalNPC.AttackCommitted = (slashing && NPC.ai[3] >= 26 && NPC.ai[3] <= 35)
                || (jumpSlashing && NPC.ai[1] <= 455)
                || stabbing;

            // Pre-emptive block: in neutral, a chance to raise the guard when a threat (incoming shot / close player)
            // is detected — before the hit lands.
            if (!slashing && !jumpSlashing && !stabbing && !shielding && grounded)
                tsorcRevampAIs.TryPreemptiveBlock(NPC, globalNPC);

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

            // basic slash (ai[3])
            if (NPC.ai[3] < 10)
                ++NPC.ai[3];

            if (!jumpSlashing && !stabbing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) < 55 && los)
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
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -66), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                            else
                            {
                                if (!grounded)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -66), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -20), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
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

            // jump-slash timer + eye dust telegraph
            if (NPC.ai[1] < 420)
                ++NPC.ai[1];

            // Eye-dust "primed" tell: the 60 ticks before the attack can commit (ai[1] reaches 420). Big dust for
            // the first half, small for the second. Once committed at 420 the red/yellow flash takes over.
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

            if (!slashing && !stabbing)
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

                    if (NPC.ai[1] == 445) // leap (25 ticks after the commit-flash)
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
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(24, -20), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            else
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), projSlash, (int)(lothricDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
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

            // dash stab
            if (!slashing && !jumpSlashing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 300 && NPC.Distance(player.Center) >= 150
                    && grounded && Math.Abs(NPC.Center.Y - player.Center.Y) < 6.5f * 16 && los)
                {
                    stabbing = true;
                    shielding = false;
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Yellow));
                }

                if (stabbing)
                {
                    ++NPC.ai[1];

                    if (NPC.ai[1] < 445) // 25-tick hyper-armored wind-up after the yellow commit-flash (420) before the dash
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

                    if (NPC.ai[1] == 445) // dash + stab
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 1.0f, PitchVariance = 0.3f }, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), projStab, (int)(lothricDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X += 10.5f;
                                NPC.velocity.Y -= 2f;
                            }
                            else
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-44, -2), new Vector2(0, 0), projStab, (int)(lothricDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X -= 10.5f;
                                NPC.velocity.Y -= 2f;
                            }
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

                    if (NPC.ai[1] > 489)
                    {
                        NPC.ai[1] = 280;
                        stabbing = false;
                    }
                }
            }

            // Shielding is REACTIVE only: the pre-emptive + on-hit block (ReactiveBlockTimer); the old autonomous
            // ai[2] timer metronome is gone. While guarding, plant in place on the idle shield frame.
            shielding = globalNPC.ReactiveBlockTimer > 0 && !jumpSlashing && !slashing && !stabbing && NPC.velocity.Y == 0;
            if (shielding)
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
                NPC.velocity.X = 0;
            }

            globalNPC.ShieldGuarding = shielding;
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

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            int shieldPower = NPC.life < NPC.lifeMax / 2 ? NPC.defense * 4 : NPC.defense * 3;

            if (shielding)
            {
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 340) NPC.ai[2] -= 35;
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 340) NPC.ai[2] -= 35;
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

            int shieldPower = NPC.life < NPC.lifeMax / 2 ? NPC.defense * 4 : NPC.defense * 3;

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
                            if (NPC.ai[2] > 340) NPC.ai[2] -= 35;
                        }
                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 80;
                            if (NPC.ai[2] > 340) NPC.ai[2] -= 35;
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
                            if (NPC.ai[2] > 350) NPC.ai[2] -= 35;
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) NPC.ai[1] += 80;
                            if (NPC.ai[2] > 340) NPC.ai[2] -= 35;
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
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            if (spawnInfo.Water) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (tsorcRevampWorld.SuperHardMode && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return 0.002f;

            if (spawnInfo.Player.ZoneDungeon) return chance = 0.001f;

            if (Main.hardMode && spawnInfo.Player.ZoneDungeon) return chance = 0.005f;

            if (NPC.downedBoss3 && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.00003f;

            return chance;
        }

        public override void OnKill()
        {
            if (!tsorcRevampWorld.SuperHardMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight, 1));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Items.Potions.RadiantLifegem>(), 2, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.LostUndeadSoul>(), 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.RagePotion, 13));
            npcLoot.Add(ItemDropRule.Common(ItemID.WrathPotion, 13));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 36, 42));
        }

        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor)
        {
            drawColor = new Color(drawColor.ToVector3() * Color.DimGray.ToVector3());
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
            Texture2D CrimsonEquipment = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/LothricKnight_CrimsonEquipment");
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/LothricKnight_Shield");
            Rectangle myrectangle = shieldTexture.Frame(1, 15, 0, shieldFrame);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.life < NPC.lifeMax / 2)
            {
                if (NPC.spriteDirection == 1)
                    spriteBatch.Draw(CrimsonEquipment, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), Color.Crimson * 0.8f, NPC.rotation, new Vector2(32, 32), NPC.scale, effects, 0f);
                else
                    spriteBatch.Draw(CrimsonEquipment, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), Color.Crimson * 0.8f, NPC.rotation, new Vector2(43, 32), NPC.scale, effects, 0f);
            }

            if (shielding && !jumpSlashing && !slashing && !stabbing)
            {
                Color shieldColor = NPC.life < NPC.lifeMax / 2 ? Color.Crimson * 0.8f : lightColor;
                spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, shieldColor, NPC.rotation, new Vector2(43, 32), NPC.scale, effects, 0f);
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

                if (NPC.ai[3] < 18) NPC.frame.Y = 11 * frameHeight;
                else if (NPC.ai[3] < 26) NPC.frame.Y = 12 * frameHeight;
                else if (NPC.ai[3] < 29) NPC.frame.Y = 13 * frameHeight;
                else if (NPC.ai[3] < 32) NPC.frame.Y = 14 * frameHeight;
                else if (NPC.ai[3] < 35) NPC.frame.Y = 15 * frameHeight;
                else if (NPC.ai[3] < 49) NPC.frame.Y = 16 * frameHeight;
            }

            if (jumpSlashing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 437) NPC.frame.Y = 11 * frameHeight;
                else if (NPC.ai[1] < 445) NPC.frame.Y = 12 * frameHeight;
                else if (NPC.ai[1] < 448) NPC.frame.Y = 13 * frameHeight;
                else if (NPC.ai[1] < 451) NPC.frame.Y = 14 * frameHeight;
                else if (NPC.ai[1] < 454) NPC.frame.Y = 15 * frameHeight;
                else if (NPC.ai[1] < 489) NPC.frame.Y = 16 * frameHeight;
            }

            if (stabbing)
            {
                NPC.spriteDirection = NPC.direction;

                if (NPC.ai[1] < 445) NPC.frame.Y = 2 * frameHeight;
                else if (NPC.ai[1] < 470) NPC.frame.Y = 17 * frameHeight;
                else if (NPC.ai[1] < 475) NPC.frame.Y = 15 * frameHeight;
                else if (NPC.ai[1] < 489) NPC.frame.Y = 16 * frameHeight;
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing && !stabbing)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding)
            {
                shieldFrame = shieldAnimTimer / 4;
                if (shieldFrame == 0) countingUP = true;
                if (shieldFrame <= 14 && countingUP) shieldAnimTimer++;
                if (shieldFrame == 14) countingUP = false;
                if (shieldFrame >= 0 && !countingUP) shieldAnimTimer--;
            }
        }

        #endregion
    }
}
