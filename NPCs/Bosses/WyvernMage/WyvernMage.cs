using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Utilities;
using static tsorcRevamp.NPCs.VanillaChanges;

namespace tsorcRevamp.NPCs.Bosses.WyvernMage
{
    [AutoloadBossHead]
    class WyvernMage : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 6;
            NPC.width = 32;
            NPC.height = 48;
            NPC.damage = 0;
            NPC.defense = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 18200;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 150000;
            NPC.rarity =18;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.WyvernMage.DespawnHandler"), Color.DarkCyan, DustID.Demonite);
            nextWarpPoint = Main.rand.NextVector2CircularEdge(320, 320);
        }


        int frozenSawDamage = 39;
        int lightningDamage = 49;

        int plasmaDamage = 44;
        int lifeTimer = 0;

        int holdTimer = 0;
        int lightningDelayTimer = -1; 
        Vector2 delayedTargetPosition;

        //When this hits 5, the boss fires an orb and resets it back to 0. Only happens right at the start of its teleport.
        public int OrbTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //When this hits 200 (120 if dragon is dead) the boss teleports
        public int TeleportTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        //Counts up each time the boss fires an orb.
        public int ShotCount
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.WitheredWeapon, 3 * 60, false);
            target.AddBuff(BuffID.WitheredArmor, 3 * 60, false);
        }

        #region AI
        NPCDespawnHandler despawnHandler;

        bool dragonAliveLastFrame = true;
        int phaseTransitionTimer = 0;
        float spawnDelay = 60;
        float zapTimer = 45;
        bool initialized = false;
        int normalAttackCounter = 0;
        public override void AI()
        {
            Lighting.AddLight(NPC.Center, Color.BlueViolet.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if(!initialized)
            {
                nextWarpPoint = Main.rand.NextVector2CircularEdge(640, 440) + NPC.Center;
                initialized = true;
            }

            if(deathTimer > 0)
            {
                deathTimer++;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int rand = Main.rand.Next(10);
                    if (rand == 0)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.Cyan));
                    }
                    if (rand == 1)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.BlueViolet));
                    }
                    if(rand == 2)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(10, 10), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(new Color(0, 30, 255)));
                    }
                }

                if(deathTimer == 120)
                {
                    for(int i = 0; i < 16; i++)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0.7f, 0.7f).RotatedBy(MathHelper.PiOver2 * Main.rand.Next(0, 4)), ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), 0, 0f, Main.myPlayer);
                    }
                }

                if (deathTimer > 180)
                {
                    NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);
                }

                return;
            }




            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            //Proximity Debuffs
            if (NPC.life < NPC.lifeMax / 2)//NPC.Distance(player.Center) < 3550 &&
            {
                player.AddBuff(BuffID.Chilled, 30, false);
                player.AddBuff(BuffID.Ichor, 30, false);
                //player.AddBuff(ModContent.BuffType<Buffs.Chilled>(), 60, false);

                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMage.FrostWave"), 235, 199, 23);//deep yellow
                    holdTimer = 12000;
                }
            }

            //Count up the timers
            OrbTimer++;
            TeleportTimer++;

            //Check if the dragon is alive. If so, spawn less transparent dusts and fire more orbs
            bool dragonAlive = NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());

            if (dragonAlive)
            {
                NPC.defense = 40; 
            }

            if(dragonAliveLastFrame && !dragonAlive)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMage.DragonDead"), 235, 199, 23);//deep yellow
                phaseTransitionTimer = 1;
            }
            dragonAliveLastFrame = dragonAlive;

            if(phaseTransitionTimer > 0 && phaseTransitionTimer < 360)
            {
                if (phaseTransitionTimer == 300)
                {
                    Vector2 spawnVec = new Vector2(500, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        auraBonus = 1.8f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            spawnVec = spawnVec.RotatedBy(MathHelper.PiOver2);
                            Vector2 aimVec = -Vector2.Normalize(spawnVec);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnVec, aimVec, ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }
                phaseTransitionTimer++;
                OrbTimer++;
                return;
            }

            int transparency = 100;
            if (!dragonAlive)
            {
                NPC.defense = 0;
                transparency += 50;
            }
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X, NPC.velocity.Y, transparency, Color.Black, 1f);
            Main.dust[dust].noGravity = true;

            if (nextAttackType == 0)
            {
                if ((OrbTimer >= 5 && ShotCount < 5) || (ShotCount < 7 && !dragonAlive))
                {
                    auraBonus = 1;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 startPos = NPC.Center;
                        float speed;
                        float startRotation;

                        //Set the initial shot rotation offset and speed
                        if (dragonAlive)
                        {
                            speed = 4;
                            startRotation = MathHelper.ToRadians(-15);
                        }
                        else
                        {
                            speed = 3f;
                            startRotation = MathHelper.ToRadians(-60);
                        }

                        //Generate the velocity vector
                        Vector2 projVelocity = UsefulFunctions.Aim(startPos, Main.player[NPC.target].Center, speed);

                        //Rotate it by the initial rotation
                        //Rotate it again by 15 degrees per shot
                        projVelocity = projVelocity.RotatedBy(startRotation + MathHelper.ToRadians(15) * ShotCount);

                        //Fire it
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), startPos.X, startPos.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), frozenSawDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    OrbTimer = 0;
                    ShotCount++;
                }

                if (TeleportTimer % 30 == 0 && TeleportTimer < 180 && TeleportTimer > 60 && !dragonAlive)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        delayedTargetPosition = new Vector2(Main.player[NPC.target].Center.X, Main.player[NPC.target].Center.Y - 100);
                        lightningDelayTimer = 20;
                    }
                }

                if (lightningDelayTimer > 0)
                {
                    lightningDelayTimer--;
                    if (lightningDelayTimer == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawnPos = delayedTargetPosition;
                            spawnPos.X += Main.rand.Next(-50, 50);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Bolt>(), frozenSawDamage, 0f, Main.myPlayer);
                            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1f, Pitch = 0f }, spawnPos); 
                        }
                        lightningDelayTimer = -1;
                    }
                }
            }

            else if (nextAttackType == 1)
            {
                if (TeleportTimer == 50)
                {
                    auraBonus = 1;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                        if (dragonAlive)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.JellyfishLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel.RotatedBy(-0.15f) * 4, ModContent.ProjectileType<Projectiles.Ice4Icicle>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel.RotatedBy(0.15f) * 4, ModContent.ProjectileType<Projectiles.Ice4Icicle>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                        }
                    }
                }

                if (TeleportTimer % 70 == 0 && !dragonAlive)
                {
                    auraBonus = 0.5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 projVel = new Vector2(Main.rand.NextFloat(-.3f, .3f), 5);
                            Vector2 spawnPos = Main.player[NPC.target].Center;
                            spawnPos.Y += -600;
                            spawnPos.X += -800 + (250 * i);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, projVel, ModContent.ProjectileType<Projectiles.Enemy.FrozenSaw>(), frozenSawDamage, 0f, Main.myPlayer);
                        }
                    }
                }                
            }
            else if (nextAttackType == 2)
            {
                zapTimer--;
                Vector2 spawnPos = Main.rand.NextVector2CircularEdge(1500, 1500) + NPC.Center;
                Vector2 projVel = UsefulFunctions.Aim(spawnPos, NPC.Center, 9);
                projVel = projVel.RotatedBy(-.25 + 2f * (teleportCooldown - TeleportTimer) / 600);
                //Main.NewText(2f * (teleportCooldown - TeleportTimer) / 600);
                if (TeleportTimer < 400)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, projVel, ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.WyvernIcicle>(), frozenSawDamage, 0f, Main.myPlayer, 1);
                    }
                }

                if (zapTimer <= 0 && TeleportTimer > 90)
                {
                    auraBonus = .8f;
                    zapTimer = Main.rand.Next(45, 80);
                    projVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>(), frozenSawDamage, 0f, Main.myPlayer);
                    }
                }
            }
            //Dramatically slow down shortly after teleporting
            if (TeleportTimer >= 10)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.27f;
            }

            //Every so often, teleport. Happens every 200 frames if the wyvern is alive, 120 if not
            if (TeleportTimer >= teleportCooldown)
            {
                WyvernMageTeleport();
            }

            //end of W1k's Death code

            if (TeleportTimer == 10) //If the boss just teleported
            {
                if (NPC.life <= 12000 && lifeTimer < 1 || NPC.life <= 7000 && lifeTimer < 2)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.WyvernMage.Archdeacons"), 175, 75, 255);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int Paraspawn = 0;
                        Vector2 spawnLoc = Main.player[NPC.target].Center;

                        //do NOT spawn the archdeacons inside a wall, ensure there is a clear line of sight between their spawn location and the player
                        if (Collision.CanHitLine(spawnLoc, 1, 1, spawnLoc + new Vector2(636, 0), 1, 1))
                        {
                            Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnLoc.X + 636, (int)spawnLoc.Y, ModContent.NPCType<Enemies.Archdeacon>(), 0);
                            Main.npc[Paraspawn].velocity.X = NPC.velocity.X;

                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn, 0f, 0f, 0f, 0);
                            }
                        }
                        if (Collision.CanHitLine(spawnLoc, 1, 1, spawnLoc + new Vector2(-636, 0), 1, 1))
                        {
                            Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnLoc.X - 636, (int)spawnLoc.Y, ModContent.NPCType<Enemies.Archdeacon>(), 0);
                            Main.npc[Paraspawn].velocity.X = NPC.velocity.X;

                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn, 0f, 0f, 0f, 0);
                            }
                        }
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    lifeTimer++;
                }

                if (Main.rand.NextBool(60) || (dragonAlive && Main.rand.NextBool(40)))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.life > NPC.lifeMax / 2)
                    {
                        int Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                        int Paraspawn2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<Enemies.BarrowWight>(), 0);
                        Main.npc[Paraspawn2].velocity.X = NPC.velocity.X;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn, 0f, 0f, 0f, 0);
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Paraspawn2, 0f, 0f, 0f, 0);
                        }
                    }
                }
            }
        }

        #endregion revamped

        Vector2 nextWarpPoint;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(nextWarpPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextWarpPoint = reader.ReadVector2();
        }

        int teleportCooldown = 260;
        int nextAttackType = 0;
        public void WyvernMageTeleport()
        {
            if (nextWarpPoint != Vector2.Zero)
            {
                Vector2 diff = nextWarpPoint - NPC.Center;
                float length = diff.Length();
                diff.Normalize();
                Vector2 offset = Vector2.Zero;

                for (int i = 0; i < length; i++)
                {
                    offset += diff;
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 dustPoint = offset;
                        dustPoint.X += Main.rand.NextFloat(-NPC.width / 2, NPC.width / 2);
                        dustPoint.Y += Main.rand.NextFloat(-NPC.height / 2, NPC.height / 2);
                        if (Main.rand.NextBool())
                        {
                            Dust.NewDustPerfect(NPC.Center + dustPoint, 71, diff * 5, 200, default, 0.8f).noGravity = true;
                        }
                        else
                        {
                            Dust.NewDustPerfect(NPC.Center + dustPoint, DustID.FireworkFountain_Pink, diff * 5, 200, default, 0.8f).noGravity = true;
                        }
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), nextWarpPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);
                }

                NPC.Center = nextWarpPoint;
            }

            NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 13);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            TeleportTimer = 0;
            ShotCount = 0;


            nextAttackType = Main.rand.Next(2);

            //Faster teleports when the dragon is dead
            if(!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
            {
                teleportCooldown = 300;

                //Add third attack, weighted
                nextAttackType = Main.rand.Next(100);
                if (nextAttackType < 40)
                {
                    nextAttackType = 0;
                    normalAttackCounter++;
                }
                if (nextAttackType >= 40 && nextAttackType < 80)
                {
                    nextAttackType = 1;
                    normalAttackCounter++;
                }
                if (nextAttackType >= 80 || normalAttackCounter > 5)
                {
                    normalAttackCounter = 0;
                    nextAttackType = 2;
                    teleportCooldown = 600;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                Main.dust[dust].noGravity = false;
            }

            int warpHorizontalMax = 640;
            int warpVerticalMax = 440;

            if(nextAttackType == 2)
            {
                warpHorizontalMax = 400;
                warpVerticalMax = 10;
            }


            //Check if the player has line of sight to the warp point. If not, rotate it by 90 degrees and try again. After 15 checks, give up.
            //Imperfect, but works for 99% of cases so it works.
            int checks = 0;
            while(checks < 15)
            {
                checks++;
                nextWarpPoint = Main.rand.NextVector2CircularEdge(warpHorizontalMax, warpVerticalMax);
                if (Collision.CanHit(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1) || Collision.CanHitLine(Main.player[NPC.target].Center + nextWarpPoint, 1, 1, Main.player[NPC.target].Center, 1, 1))
                {
                    nextWarpPoint = Main.player[NPC.target].Center + nextWarpPoint;
                    break;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), nextWarpPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.TeleportTelegraph>(), 0, 0, Main.myPlayer, ai1: teleportCooldown);
            }
            
            NPC.netUpdate = true;
        }
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
            {
                modifiers.FinalDamage *= 0.5f; 
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
            {
                modifiers.FinalDamage *= 0.5f; 
            }
        }
        public override void FindFrame(int currentFrame)
        {
            int frameHeight = !Main.dedServ ? TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] : 1;

            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
            {
                NPC.frame.Y = 2 * frameHeight; 
                NPC.frameCounter = 0; 
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
                return;
            }

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.WyvernMageBag>(), 1, 1, 1, new WyvernMageDropCondition()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 2, 4));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LampTome>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GemBox>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.AngelWings));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.DemonWings));
            npcLoot.Add(notExpertCondition);
        }

        int deathTimer = 0;
        public override bool CheckDead()
        {
            if (deathTimer == 0)
            {
                SoundEngine.PlaySound(SoundID.Shatter);
                deathTimer++;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
            }

            if (deathTimer > 180)
            {
                return true;
            }
            return false;
        }

        public override void OnKill()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 1400, 80);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 900, 60);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 600, 60);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 400, 60);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 200, 60);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wyvern Mage Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wyvern Mage Gore 2").Type, 1f);
            }

            if (!Main.expertMode)
            {
                if (!(tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMage>()))))
                { //If the boss has not yet been killed
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 15000); //Then drop the souls
                }
            }
        }


        float auraBonus;
        int expandRadius = 0;
        public static Effect effect;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            auraBonus *= 0.9f;

            Color rgbColor = Color.Purple;
            if(nextAttackType == 0)
            {
                expandRadius = 0;
            }
            if(nextAttackType == 1)
            {
                expandRadius = 0;
                rgbColor = Color.Cyan;
            }
            if(nextAttackType == 2)
            {
                if (expandRadius < 1000)
                {
                    expandRadius += 2;
                }
                rgbColor = new Color(0, 30, 255);
            }
            
            if(dragonAliveLastFrame)
            {
                rgbColor = Color.WhiteSmoke;
            }


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float effectIntensity = 0;
            if (!dragonAliveLastFrame)
            {
                effectIntensity = 1;
                int effectTimer = 1;//Placeholder
                if (effectTimer > 540)
                {
                    effectIntensity = 1f - ((effectTimer - 540f) / 60f);
                }

                if (effectTimer < 20)
                {
                    effectIntensity = effectTimer / 20f;
                }
            }

            float timeFactor = 1;
            float scaleFactor = 4;

            //Apply the shader, caching it as well
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            float colorIntensity = 0.25f;
            if (phaseTransitionTimer > 1)
            {
                colorIntensity = (.6f * ((float)phaseTransitionTimer) / 360f) + auraBonus;
                rgbColor = Color.Lerp(Color.Gray, rgbColor, colorIntensity);
            }


            Rectangle sourceRectangle = new Rectangle(0, 0, 150 + expandRadius/4 + (int)(70 * (effectIntensity + auraBonus)), 150 + expandRadius/4 + (int)(70 * (effectIntensity + auraBonus)));
            Vector2 origin = sourceRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseVoronoi.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4() * colorIntensity * 0.5f * (6 + (expandRadius / 500f)));
            effect.Parameters["ringProgress"].SetValue(0.4f + .25f * effectIntensity + (expandRadius / 2500f));
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            effect.Parameters["scaleFactor"].SetValue(scaleFactor);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseVoronoi, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
    }

    public class WyvernMageDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<MechaDragonHead>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return LangUtils.GetTextValue("NPCs.WyvernMage.Condition");
        }
    }
}