using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.BossItems;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Bosses.Attraidies;
using tsorcRevamp.Systems;

namespace tsorcRevamp.NPCs.Special
{
    /// <summary>
    /// The Attraidies arena ring. Entering it with the Mind Cube starts the fight after a short arrival scene. Entering it
    /// without the Cube plays the curse scene, which blasts everyone nearby to 1 HP and throws them away.
    /// </summary>
    public static class AttraidiesCutscenes
    {
        public const string CurseScene = "AttraidiesCurse";
        public const string ArrivalScene = "AttraidiesArrival";
        public const string SorrowScene = "SorrowIntro";

        // Scenes that wait on dialogue start open-ended and close themselves; this only guards against a stuck script.
        public const int OpenEndedSceneTicks = 60 * 60 * 10;
        private const float AudienceRadius = 60f * 16f;
        private const float ActorForwardDistance = 18f * 16f;
        private const float ActorHoverHeight = 14f * 16f;

        // The solid arena floor spans about 60 tiles either side of the ring center.
        private const float ActorMaxRingOffset = 60f * 16f;
        private const float BossSpawnHeight = 20f * 16f;
        private const int ArrivalZoomStartTick = 20;
        private const int ArrivalZoomTicks = 45;
        private const float ArrivalZoom = 1.5f;
        private const int ArrivalLineAutoTicks = 6 * 60;
        private const int BarCloseTicks = 20;
        private const int ChainGoneGraceTicks = 120;

        private enum RingScript
        {
            None,
            Curse,
            ArrivalZoomIn,
            ArrivalLine,
            ArrivalZoomOut,
            FightWatch,
        }

        private static RingScript activeScript;
        private static int scriptTimer;
        private static int scriptLead;
        private static int curseActorIndex;
        private static int chainGoneTicks;

        public static bool RingCondition()
        {
            return tsorcScriptedEvents.OnlyAdventureMapCondition()
                && !tsorcRevampWorld.BossDefeated(ModContent.NPCType<BrokenOkiku>())
                && !AnyAttraidiesFormAlive()
                && !CutsceneSystem.IsActive;
        }

        private static bool AnyAttraidiesFormAlive()
        {
            return NPC.AnyNPCs(ModContent.NPCType<DarkShogunMask>())
                || NPC.AnyNPCs(ModContent.NPCType<DarkDragonMask>())
                || NPC.AnyNPCs(ModContent.NPCType<Okiku>())
                || NPC.AnyNPCs(ModContent.NPCType<BrokenOkiku>())
                || NPC.AnyNPCs(ModContent.NPCType<Attraidies>());
        }

        /// <summary>Anywhere on the player counts: inventory, accessory slots, vanity slots and modded accessory slots.</summary>
        public static bool CarriesMindCube(Player player)
        {
            int cubeType = ModContent.ItemType<MindCube>();
            if (player.HasItem(cubeType))
            {
                return true;
            }

            // armor[3..] are accessories and armor[13..] their vanity counterparts, both extended by extra slots.
            int accessorySlotCount = 6 + player.extraAccessorySlots;
            for (int i = 0; i < accessorySlotCount; i++)
            {
                if (player.armor[3 + i].type == cubeType || player.armor[13 + i].type == cubeType)
                {
                    return true;
                }
            }

            ModAccessorySlotPlayer modSlotPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
            AccessorySlotLoader slotLoader = LoaderManager.Get<AccessorySlotLoader>();
            for (int slotIndex = 0; slotIndex < modSlotPlayer.SlotCount; slotIndex++)
            {
                ModAccessorySlot modSlot = slotLoader.Get(slotIndex, player);
                if (modSlot == null)
                {
                    continue;
                }

                if (modSlot.FunctionalItem.type == cubeType || modSlot.VanityItem.type == cubeType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The lead plus every living player near center. Skip is offered once the world has seen this scene.</summary>
        public static CutsceneCast BuildCast(Vector2 center, int leadPlayer, string sceneId)
        {
            CutsceneCast cast = new CutsceneCast();
            cast.LeadPlayer = leadPlayer;
            cast.AllowSkip = CutsceneSystem.HasSeen(sceneId);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool inRange = player.active && !player.dead && player.Distance(center) <= AudienceRadius;
                if (inRange || i == leadPlayer)
                {
                    cast.Players.Add(i);
                }
            }

            return cast;
        }

        /// <summary>First solid or platform surface at or below startY, in world pixels. Falls back to startY.</summary>
        public static float FindFloorY(float worldX, float startY)
        {
            int tileX = (int)(worldX / 16f);
            int tileY = (int)(startY / 16f);

            for (int offset = 0; offset < 100; offset++)
            {
                Tile tile = Main.tile[tileX, tileY + offset];
                bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (standable)
                {
                    return (tileY + offset) * 16f;
                }
            }

            return startY;
        }

        /// <summary>Runs on the server or in single player, once per tick, from the moment a player enters the ring.</summary>
        public static EventActionStatus RingAction(ScriptedEvent ringEvent)
        {
            if (ringEvent.eventTimer == 0)
            {
                activeScript = RingScript.None;
                scriptTimer = 0;
                chainGoneTicks = 0;
                scriptLead = ringEvent.TriggeringPlayer;

                bool leadValid = scriptLead >= 0 && scriptLead < Main.maxPlayers && Main.player[scriptLead].active && !Main.player[scriptLead].dead;
                if (!leadValid)
                {
                    return EventActionStatus.FailedEvent;
                }

                Vector2 ringCenter = ringEvent.centerpoint;
                CutsceneCast previewCast = BuildCast(ringCenter, scriptLead, ArrivalScene);

                // The ring's own trigger player leads when they hold the Cube; otherwise any nearby holder does.
                int cubeHolder = -1;
                foreach (int playerIndex in previewCast.Players)
                {
                    bool holdsCube = CarriesMindCube(Main.player[playerIndex]);
                    if (holdsCube && (cubeHolder < 0 || playerIndex == scriptLead))
                    {
                        cubeHolder = playerIndex;
                    }
                }

                if (cubeHolder >= 0)
                {
                    scriptLead = cubeHolder;
                    Player holder = Main.player[cubeHolder];
                    float floorY = FindFloorY(ringCenter.X, holder.Bottom.Y - 8f);
                    Vector2 bossPosition = new Vector2(ringCenter.X, floorY - BossSpawnHeight);

                    NPC.NewNPC(NPC.GetBossSpawnSource(cubeHolder), (int)bossPosition.X, (int)bossPosition.Y, ModContent.NPCType<DarkShogunMask>());
                    Projectile.NewProjectile(new EntitySource_BossSpawn(holder), bossPosition, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                    Projectile.NewProjectile(new EntitySource_BossSpawn(holder), bossPosition, Vector2.Zero, ModContent.ProjectileType<ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);

                    CutsceneCast arrivalCast = BuildCast(ringCenter, cubeHolder, ArrivalScene);
                    CutsceneSystem.Start(ArrivalScene, OpenEndedSceneTicks, CutsceneFocus.OnPlayer(cubeHolder), -1, arrivalCast);
                    activeScript = RingScript.ArrivalZoomIn;
                }
                else
                {
                    Player lead = Main.player[scriptLead];
                    int facing = lead.direction;

                    // He appears in front of the player, so being blasted away from him throws them backwards.
                    float actorX = lead.Center.X + facing * ActorForwardDistance;
                    actorX = MathHelper.Clamp(actorX, ringCenter.X - ActorMaxRingOffset, ringCenter.X + ActorMaxRingOffset);
                    float floorY = FindFloorY(actorX, lead.Bottom.Y - 8f);

                    curseActorIndex = NPC.NewNPC(NPC.GetBossSpawnSource(scriptLead), (int)actorX, (int)(floorY - ActorHoverHeight), ModContent.NPCType<AttraidiesArenaActor>(), 0, 0f, 0f, floorY, -facing);

                    if (curseActorIndex >= Main.maxNPCs)
                    {
                        return EventActionStatus.FailedEvent;
                    }

                    CutsceneCast curseCast = BuildCast(ringCenter, scriptLead, CurseScene);
                    CutsceneFocus curseFocus = CutsceneFocus.BetweenLocalPlayerAndNpc(curseActorIndex);
                    CutsceneSystem.Start(CurseScene, OpenEndedSceneTicks, curseFocus, curseActorIndex, curseCast);
                    activeScript = RingScript.Curse;
                }
            }

            scriptTimer++;

            switch (activeScript)
            {
                case RingScript.Curse:
                    NPC actor = Main.npc[curseActorIndex];
                    bool actorPresent = actor.active && actor.type == ModContent.NPCType<AttraidiesArenaActor>();
                    if (!actorPresent)
                    {
                        // Failing the event sends the ring through its normal re-arm delay.
                        return EventActionStatus.FailedEvent;
                    }
                    break;

                case RingScript.ArrivalZoomIn:
                    if (scriptTimer == ArrivalZoomStartTick)
                    {
                        CutsceneSystem.SetZoom(ArrivalZoom, ArrivalZoomTicks);
                    }

                    if (scriptTimer >= ArrivalZoomStartTick + ArrivalZoomTicks)
                    {
                        CutsceneLine arrivalLine = CutsceneLine.FromPlayer(scriptLead, "Cutscenes.Speakers.Red", "Cutscenes.Arrival.Red");
                        CutsceneSystem.ShowLine(arrivalLine, ArrivalLineAutoTicks);
                        activeScript = RingScript.ArrivalLine;
                        scriptTimer = 0;
                    }
                    break;

                case RingScript.ArrivalLine:
                    if (CutsceneSystem.LineFinished)
                    {
                        CutsceneSystem.ClearLine();
                        CutsceneSystem.SetZoom(1f, ArrivalZoomTicks);
                        activeScript = RingScript.ArrivalZoomOut;
                        scriptTimer = 0;
                    }
                    break;

                case RingScript.ArrivalZoomOut:
                    if (scriptTimer >= ArrivalZoomTicks)
                    {
                        CutsceneSystem.FinishIn(BarCloseTicks);
                        CutsceneSystem.MarkSeen(ArrivalScene);
                        activeScript = RingScript.FightWatch;
                    }
                    break;

                case RingScript.FightWatch:
                    // Forms hand over within a tick or two; the grace period keeps a transform from reading as a wipe.
                    if (AnyAttraidiesFormAlive())
                    {
                        chainGoneTicks = 0;
                        break;
                    }

                    chainGoneTicks++;
                    if (chainGoneTicks >= ChainGoneGraceTicks)
                    {
                        if (tsorcRevampWorld.BossDefeated(ModContent.NPCType<BrokenOkiku>()))
                        {
                            return EventActionStatus.CompletedEvent;
                        }

                        return EventActionStatus.FailedEvent;
                    }
                    break;
            }

            bool arrivalRunning = activeScript == RingScript.ArrivalZoomIn || activeScript == RingScript.ArrivalLine || activeScript == RingScript.ArrivalZoomOut;
            if (arrivalRunning && CutsceneSystem.SkipRequested)
            {
                CutsceneSystem.ClearLine();
                CutsceneSystem.SetZoom(1f, BarCloseTicks);
                CutsceneSystem.FinishIn(BarCloseTicks);
                activeScript = RingScript.FightWatch;
            }

            return EventActionStatus.Continue;
        }
    }

    /// <summary>Attraidies as he appears in the curse scene: harmless, scripted, and gone when the scene ends.</summary>
    public class AttraidiesArenaActor : AttraidiesPuppet
    {
        private enum ActorStage
        {
            Forming,
            Taunt,
            Cast,
            Channel,
            Descend,
            Vanish,
        }

        public const int CastTicks = 40;
        private const int FormingTicks = 75;
        private const int FadeInTicks = 60;
        private const int LaughTick = 20;
        private const int TauntAutoTicks = 8 * 60;
        private const int DescendTicks = 120;
        private const int VanishTicks = 45;
        private const int BarCloseTicks = 20;

        // Facing-space angles for the free hand: reaching toward the player, then closing upward as the curse charges.
        private const float TauntHandAngle = -0.15f;
        private const float ChannelHandStartAngle = 0.6f;
        private const float ChannelHandEndAngle = -0.35f;

        private ActorStage Stage => (ActorStage)(int)NPC.ai[0];

        protected override bool ShowsTeleportFlash => false;
        protected override Vector2 FacingPoint => NPC.Center + new Vector2(NPC.ai[3] * 100f, 0f);

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            NPCID.Sets.NPCBestiaryDrawModifiers hiddenFromBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers();
            hiddenFromBestiary.Hide = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hiddenFromBestiary);
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.defense = 0;
            NPC.lifeMax = 1000;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.npcSlots = 0f;
            NPC.alpha = 255;
            NPC.ShowNameOnHover = false;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        private void SetStage(ActorStage stage)
        {
            NPC.ai[0] = (float)stage;
            NPC.ai[1] = 0f;
            NPC.netUpdate = true;
        }

        private void SpawnBolts(int startMode, int startTimer)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !CutsceneSystem.IsParticipant(player))
                {
                    continue;
                }

                int boltIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), StaffTip, Vector2.Zero, ModContent.ProjectileType<AttraidiesCurseBolt>(), 0, 0f, Main.myPlayer, AttraidiesCurseBolt.ModeForming, i, 0f);
                if (boltIndex >= Main.maxProjectiles)
                {
                    continue;
                }

                AttraidiesCurseBolt bolt = (AttraidiesCurseBolt)Main.projectile[boltIndex].ModProjectile;
                bolt.ActorIndex = NPC.whoAmI;
                Main.projectile[boltIndex].netUpdate = true;

                if (startMode == AttraidiesCurseBolt.ModeAttached)
                {
                    bolt.Attach(startTimer);
                }
            }
        }

        private void CountBolts(out int livingBolts, out int attachedBolts)
        {
            livingBolts = 0;
            attachedBolts = 0;
            int boltType = ModContent.ProjectileType<AttraidiesCurseBolt>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != boltType)
                {
                    continue;
                }

                livingBolts++;
                if ((int)projectile.ai[0] == AttraidiesCurseBolt.ModeAttached)
                {
                    attachedBolts++;
                }
            }
        }

        protected override void TickForm()
        {
            ActorStage stage = Stage;
            int stageTimer = (int)NPC.ai[1];
            NPC.ai[1]++;

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            bool hasVisuals = !Main.dedServ;

            NPC.velocity = Vector2.Zero;
            NPC.timeLeft = 600;
            Lighting.AddLight(NPC.Center, 0.7f, 0.35f, 1f);

            // Skip jumps straight to the blast, so the punishment always lands.
            bool beforeBlast = stage != ActorStage.Vanish;
            if (authority && beforeBlast && CutsceneSystem.SkipRequested && NPC.localAI[1] == 0f)
            {
                NPC.localAI[1] = 1f;
                CutsceneSystem.ClearLine();

                if (stage == ActorStage.Forming || stage == ActorStage.Taunt)
                {
                    SpawnBolts(AttraidiesCurseBolt.ModeAttached, AttraidiesCurseBolt.ChargeTicks - AttraidiesCurseBolt.SkipChargeTicks);
                }
                else
                {
                    int boltType = ModContent.ProjectileType<AttraidiesCurseBolt>();
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile projectile = Main.projectile[i];
                        if (projectile.active && projectile.type == boltType)
                        {
                            ((AttraidiesCurseBolt)projectile.ModProjectile).Attach(AttraidiesCurseBolt.ChargeTicks - AttraidiesCurseBolt.SkipChargeTicks);
                        }
                    }
                }

                SetStage(ActorStage.Descend);
                stage = ActorStage.Descend;
                stageTimer = 0;
            }

            // Poses follow the synced stage, so every machine shows the same gesture without extra packets.
            ScriptedStaffAngle = null;
            FreeHandGesture = null;
            Player cursedPlayer = null;
            float curseCharge = 0f;
            int curseBoltType = ModContent.ProjectileType<AttraidiesCurseBolt>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile bolt = Main.projectile[i];
                if (bolt.active && bolt.type == curseBoltType)
                {
                    cursedPlayer = Main.player[(int)bolt.ai[1]];
                    if ((int)bolt.ai[0] == AttraidiesCurseBolt.ModeAttached)
                    {
                        curseCharge = MathHelper.Clamp(bolt.ai[2] / AttraidiesCurseBolt.ChargeTicks, 0f, 1f);
                    }
                    break;
                }
            }

            switch (stage)
            {
                case ActorStage.Taunt:
                    SetPose(AttraidiesPose.Carry);
                    FreeHandGesture = TauntHandAngle;
                    break;

                case ActorStage.Cast:
                    SetPose(AttraidiesPose.Overhead);
                    float raiseProgress = MathHelper.Clamp(stageTimer / (float)CastTicks, 0f, 1f);
                    ScriptedStaffAngle = MathHelper.Lerp(CarryAngle, OverheadAngle, MathHelper.SmoothStep(0f, 1f, raiseProgress));
                    break;

                case ActorStage.Channel:
                    if (cursedPlayer != null)
                    {
                        SetAimPose(cursedPlayer.Center);
                    }
                    FreeHandGesture = ChannelHandStartAngle;
                    break;

                case ActorStage.Descend:
                    SetPose(AttraidiesPose.Carry);
                    FreeHandGesture = MathHelper.Lerp(ChannelHandStartAngle, ChannelHandEndAngle, curseCharge);
                    break;

                case ActorStage.Vanish:
                    SetPose(AttraidiesPose.Overhead);
                    break;

                default:
                    SetPose(AttraidiesPose.Carry);
                    break;
            }

            switch (stage)
            {
                case ActorStage.Forming:
                {
                    float fadeProgress = MathHelper.Clamp(stageTimer / (float)FadeInTicks, 0f, 1f);
                    NPC.alpha = (int)(255f * (1f - fadeProgress));

                    if (hasVisuals)
                    {
                        // The Sorrow scene's dust colors, in finer motes than its scale 4 puffs.
                        for (int i = 0; i < 3; i++)
                        {
                            int dustType = DustID.Shadowflame;
                            if (i % 2 == 0)
                            {
                                dustType = 54;
                            }

                            Vector2 dustVelocity = Main.rand.NextVector2Circular(5f, 5f);
                            Dust formingDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, dustVelocity.X, dustVelocity.Y, 100, default, Main.rand.NextFloat(1.1f, 1.5f));
                            formingDust.noGravity = true;
                        }

                        if (stageTimer == LaughTick)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 1.1f }, NPC.Center);
                        }

                        if (stageTimer == FadeInTicks)
                        {
                            for (int i = 0; i < 40; i++)
                            {
                                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f);
                                Dust.NewDustPerfect(NPC.Center, 54, burstVelocity, 100, default, Main.rand.NextFloat(1.2f, 1.6f)).noGravity = true;
                                Dust.NewDustPerfect(NPC.Center, DustID.Shadowflame, burstVelocity * 0.7f, 100, default, Main.rand.NextFloat(1.2f, 1.6f)).noGravity = true;
                            }
                        }
                    }

                    if (authority && stageTimer >= FormingTicks)
                    {
                        SetStage(ActorStage.Taunt);
                    }
                    break;
                }

                case ActorStage.Taunt:
                    NPC.alpha = 0;
                    if (authority && stageTimer == 0)
                    {
                        CutsceneLine taunt = CutsceneLine.FromNpc(NPC.whoAmI, "Cutscenes.Speakers.Attraidies", "Cutscenes.Curse.Taunt");
                        CutsceneSystem.ShowLine(taunt, TauntAutoTicks);
                    }

                    if (authority && CutsceneSystem.LineFinished)
                    {
                        CutsceneSystem.ClearLine();
                        SetStage(ActorStage.Cast);
                    }
                    break;

                case ActorStage.Cast:
                    NPC.alpha = 0;
                    if (authority && stageTimer == 0)
                    {
                        SpawnBolts(AttraidiesCurseBolt.ModeForming, 0);
                    }

                    if (hasVisuals)
                    {
                        if (stageTimer == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item72 with { Pitch = -0.3f }, NPC.Center);
                        }

                        float castProgress = MathHelper.Clamp(stageTimer / (float)CastTicks, 0f, 1f);
                        int suctionCount = 1 + (int)(castProgress * 3f);
                        for (int i = 0; i < suctionCount; i++)
                        {
                            Vector2 ringOffset = Main.rand.NextVector2CircularEdge(40f, 40f);
                            Vector2 inward = -ringOffset * 0.08f;
                            Dust.NewDustPerfect(StaffTip + ringOffset, DustID.ShadowbeamStaff, inward, 90, default, Main.rand.NextFloat(0.7f, 1.2f)).noGravity = true;
                        }
                    }

                    if (authority && stageTimer >= CastTicks)
                    {
                        SetStage(ActorStage.Channel);
                    }
                    break;

                case ActorStage.Channel:
                    if (authority)
                    {
                        CountBolts(out int livingBolts, out int attachedBolts);
                        if (livingBolts == 0)
                        {
                            SetStage(ActorStage.Vanish);
                        }
                        else if (attachedBolts > 0)
                        {
                            SetStage(ActorStage.Descend);
                        }
                    }
                    break;

                case ActorStage.Descend:
                {
                    NPC.alpha = 0;
                    if (NPC.localAI[0] == 0f)
                    {
                        NPC.localAI[0] = NPC.position.Y;
                    }

                    // Eases from the hover height to the floor stored in ai[2] while the charge builds.
                    float descendProgress = MathHelper.Clamp(stageTimer / (float)DescendTicks, 0f, 1f);
                    float easedDescent = descendProgress * descendProgress * (3f - 2f * descendProgress);
                    float floorPositionY = NPC.ai[2] - NPC.height;
                    NPC.position.Y = MathHelper.Lerp(NPC.localAI[0], floorPositionY, easedDescent);

                    if (hasVisuals && stageTimer == DescendTicks)
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            Vector2 puffVelocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-1f, 0f));
                            Dust.NewDustPerfect(NPC.Bottom, DustID.ShadowbeamStaff, puffVelocity, 100, default, Main.rand.NextFloat(0.8f, 1.3f)).noGravity = true;
                        }
                    }

                    if (authority)
                    {
                        CountBolts(out int livingBolts, out _);
                        if (livingBolts == 0)
                        {
                            SetStage(ActorStage.Vanish);
                        }
                    }
                    break;
                }

                case ActorStage.Vanish:
                {
                    float vanishProgress = MathHelper.Clamp(stageTimer / (float)VanishTicks, 0f, 1f);
                    NPC.alpha = (int)(255f * vanishProgress);

                    if (authority && stageTimer == 0 && CutsceneSystem.SceneId == AttraidiesCutscenes.CurseScene)
                    {
                        CutsceneSystem.FinishIn(BarCloseTicks);
                    }

                    if (hasVisuals)
                    {
                        if (stageTimer == 0)
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 1.1f, Pitch = -0.1f }, NPC.Center);
                        }

                        Vector2 vanishVelocity = Main.rand.NextVector2Circular(4f, 4f);
                        Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 54, vanishVelocity.X, vanishVelocity.Y, 100, default, 1.4f).noGravity = true;
                    }

                    if (stageTimer >= VanishTicks)
                    {
                        NPC.active = false;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                        }
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// The curse: forms at the staff tip, flies to one player, sticks to them and lifts them while it charges, then detonates.
    /// It deals no damage. It passes through tiles on purpose: this is a scripted hit that must always arrive.
    /// </summary>
    public class AttraidiesCurseBolt : ModProjectile
    {
        public const int ModeForming = 0;
        public const int ModeFlying = 1;
        public const int ModeAttached = 2;

        public const int ChargeTicks = 180;
        public const int SkipChargeTicks = 30;

        private const float FlightSpeed = 34f;
        private const int MaxFlightTicks = 14;
        private const float AttachDistance = 40f;
        private const float MaxLiftHeight = 8f * 16f;
        private const int LiftTicks = 60;

        // A client may receive the kill packet slightly before its own timer reaches the blast.
        private const int DetonateTolerance = 20;

        // Throw: 100 tiles over about 67 ticks of air. Tune the distance here.
        private const int ThrowDistanceTiles = 100;
        private const int ThrowAirTicks = 67;
        private const float ThrowLaunchVelocityY = -11f;
        private const int CrippleTicks = 6 * 60;

        public int ActorIndex = -1;
        private Vector2 liftStart;
        private float liftHeight;

        private int Mode => (int)Projectile.ai[0];
        private Player Victim => Main.player[(int)Projectile.ai[1]];
        private int ModeTimer => (int)Projectile.ai[2];

        public override string Texture => "Terraria/Images/Extra_98";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)ActorIndex);
            writer.WriteVector2(liftStart);
            writer.Write(liftHeight);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ActorIndex = reader.ReadInt16();
            liftStart = reader.ReadVector2();
            liftHeight = reader.ReadSingle();
        }

        /// <summary>Server side. Sticks to the victim, or moves an attached bolt forward to a later charge tick.</summary>
        public void Attach(int chargeTimer)
        {
            if (Mode != ModeAttached)
            {
                Player victim = Victim;
                liftStart = victim.Center;

                // Stop the lift below any ceiling, so the player is never pinned inside a tile.
                liftHeight = 0f;
                while (liftHeight < MaxLiftHeight)
                {
                    Vector2 testPosition = victim.position - new Vector2(0f, liftHeight + 8f);
                    if (Collision.SolidCollision(testPosition, victim.width, victim.height))
                    {
                        break;
                    }

                    liftHeight += 8f;
                }

                Projectile.ai[0] = ModeAttached;
                Projectile.ai[2] = chargeTimer;
            }
            else
            {
                Projectile.ai[2] = Math.Max(Projectile.ai[2], chargeTimer);
            }

            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            bool hasVisuals = !Main.dedServ;
            Player victim = Victim;
            int mode = Mode;
            int modeTimer = ModeTimer;

            Projectile.ai[2]++;
            Projectile.timeLeft = 60;
            Projectile.rotation += 0.12f;

            if (authority && (!victim.active || victim.dead))
            {
                Projectile.Kill();
                return;
            }

            switch (mode)
            {
                case ModeForming:
                {
                    bool actorValid = ActorIndex >= 0 && Main.npc[ActorIndex].active && Main.npc[ActorIndex].ModNPC is AttraidiesArenaActor;
                    if (!actorValid)
                    {
                        if (authority)
                        {
                            Projectile.Kill();
                        }
                        return;
                    }

                    AttraidiesArenaActor actor = (AttraidiesArenaActor)Main.npc[ActorIndex].ModNPC;
                    Projectile.Center = actor.StaffTip;

                    if (authority && modeTimer >= AttraidiesArenaActor.CastTicks)
                    {
                        Projectile.ai[0] = ModeFlying;
                        Projectile.ai[2] = 0f;
                        Projectile.netUpdate = true;
                    }
                    break;
                }

                case ModeFlying:
                {
                    Vector2 toVictim = victim.Center - Projectile.Center;
                    float distance = toVictim.Length();
                    float travel = Math.Min(FlightSpeed, distance);
                    Projectile.velocity = Vector2.Zero;
                    Projectile.Center += toVictim.SafeNormalize(Vector2.UnitX) * travel;

                    if (hasVisuals)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Dust trailDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.ShadowbeamStaff, Vector2.Zero, 90, default, Main.rand.NextFloat(0.8f, 1.3f));
                            trailDust.noGravity = true;
                        }

                        if (Main.rand.NextBool(3))
                        {
                            Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(2f, 2f), 90, default, 0.8f).noGravity = true;
                        }
                    }

                    if (authority && (distance <= AttachDistance || modeTimer >= MaxFlightTicks))
                    {
                        Attach(0);
                    }
                    break;
                }

                case ModeAttached:
                {
                    float chargeProgress = MathHelper.Clamp(modeTimer / (float)ChargeTicks, 0f, 1f);
                    float liftProgress = MathHelper.Clamp(modeTimer / (float)LiftTicks, 0f, 1f);
                    float easedLift = 1f - (1f - liftProgress) * (1f - liftProgress);

                    // The shake is a repeatable wave rather than a random roll, so every machine pins the same spot.
                    float shakeAmount = 0.5f + 2.5f * chargeProgress;
                    Vector2 shake = new Vector2((float)Math.Sin(modeTimer * 1.7f), (float)Math.Cos(modeTimer * 2.3f)) * shakeAmount;
                    Vector2 pinPosition = liftStart - new Vector2(0f, liftHeight * easedLift) + shake;
                    Projectile.Center = pinPosition;

                    if (modeTimer >= ChargeTicks)
                    {
                        Detonate();
                        if (authority)
                        {
                            Projectile.Kill();
                        }
                        return;
                    }

                    tsorcRevampPlayer pinnedPlayer = victim.GetModPlayer<tsorcRevampPlayer>();
                    pinnedPlayer.ImpaleWorldPosition = pinPosition;
                    pinnedPlayer.ImpaleFreezeTimer = 2;

                    // Clear anything that could fight the pin, once, on the machine that owns this player.
                    if (victim.whoAmI == Main.myPlayer && Projectile.localAI[1] == 0f)
                    {
                        Projectile.localAI[1] = 1f;
                        victim.mount.Dismount(victim);
                        victim.RemoveAllGrapplingHooks();
                        victim.pulley = false;
                        victim.sitting.SitUp(victim, false);
                        victim.sleeping.StopSleeping(victim, false);
                        victim.gravDir = 1f;
                    }

                    Lighting.AddLight(Projectile.Center, 0.25f + 0.75f * chargeProgress, 0.1f + 0.2f * chargeProgress, 0.3f + 0.9f * chargeProgress);

                    if (hasVisuals)
                    {
                        int bodyCount = 1 + (int)(chargeProgress * 5f);
                        for (int i = 0; i < bodyCount; i++)
                        {
                            Vector2 ringOffset = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(20f, 70f);
                            Vector2 inward = -ringOffset * 0.1f;
                            Dust.NewDustPerfect(Projectile.Center + ringOffset, DustID.ShadowbeamStaff, inward, 90, default, Main.rand.NextFloat(0.75f, 1.4f)).noGravity = true;
                        }

                        int sparkCount = (int)(chargeProgress * 3f);
                        for (int i = 0; i < sparkCount; i++)
                        {
                            Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f);
                            Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, sparkVelocity, 70, default, Main.rand.NextFloat(0.45f, 0.85f)).noGravity = true;
                        }

                        if (modeTimer % 3 == 0)
                        {
                            Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.Shadowflame, Main.rand.NextVector2Circular(1f, 1f), 170, new Color(35, 15, 55), Main.rand.NextFloat(0.5f, 0.8f));
                            smoke.noGravity = true;
                            smoke.noLight = true;
                            smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.7f);
                        }

                        if (modeTimer == 0 || modeTimer == 60 || modeTimer == 120 || modeTimer == 150)
                        {
                            float rise = modeTimer / (float)ChargeTicks;
                            SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -0.4f + rise, Volume = 0.6f + 0.4f * rise }, Projectile.Center);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>The blast. Runs once on every machine; each client applies its results only to its own player.</summary>
        private void Detonate()
        {
            if (Projectile.localAI[0] != 0f)
            {
                return;
            }

            Projectile.localAI[0] = 1f;
            Player victim = Victim;
            CutsceneSystem.ReleasePlayer(victim.whoAmI);

            int fallbackDirection = -victim.direction;
            int throwDirection = fallbackDirection;
            bool actorValid = ActorIndex >= 0 && Main.npc[ActorIndex].active && Main.npc[ActorIndex].ModNPC is AttraidiesArenaActor;
            if (actorValid)
            {
                float offsetFromActor = victim.Center.X - Main.npc[ActorIndex].Center.X;
                if (offsetFromActor != 0f)
                {
                    throwDirection = Math.Sign(offsetFromActor);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                CutsceneSystem.MarkSeen(AttraidiesCutscenes.CurseScene);
            }

            if (victim.whoAmI == Main.myPlayer)
            {
                // Removes existing debuffs first, so poison or fire at 1 HP cannot end the scene before the throw does.
                for (int i = Player.MaxBuffs - 1; i >= 0; i--)
                {
                    int buffType = victim.buffType[i];
                    bool removable = buffType > 0 && Main.debuff[buffType] && !BuffID.Sets.NurseCannotRemoveDebuff[buffType]
                        && !Main.buffNoTimeDisplay[buffType] && !Main.persistentBuff[buffType];
                    if (removable)
                    {
                        victim.DelBuff(i);
                    }
                }

                // Set directly rather than through Hurt, so dodges, shields and parries cannot change the result.
                victim.statLife = 1;
                victim.AddBuff(ModContent.BuffType<Crippled>(), CrippleTicks);

                CutscenePlayer cutscenePlayer = victim.GetModPlayer<CutscenePlayer>();
                cutscenePlayer.BeginThrow(throwDirection, ThrowDistanceTiles * 16f, ThrowAirTicks, ThrowLaunchVelocityY);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, victim.whoAmI);
                    NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, victim.whoAmI);
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0") with { Volume = 1.6f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.4f }, Projectile.Center);
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 14f, 6f, 30, 1800f, "AttraidiesCurse"));

            for (int i = 0; i < 70; i++)
            {
                Vector2 bodyVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f);
                Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff, bodyVelocity, 90, default, Main.rand.NextFloat(0.8f, 1.5f)).noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 14f);
                Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, sparkVelocity, 70, default, Main.rand.NextFloat(0.45f, 0.85f)).noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 smokeVelocity = Main.rand.NextVector2Circular(2.2f, 2.2f);
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), DustID.Shadowflame, smokeVelocity, 170, new Color(35, 15, 55), Main.rand.NextFloat(0.5f, 0.8f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.7f);
            }

            // A directional streak along the throw sells the force of the blast.
            for (int i = 0; i < 24; i++)
            {
                Vector2 streakVelocity = new Vector2(throwDirection * Main.rand.NextFloat(6f, 16f), Main.rand.NextFloat(-2.5f, 2.5f));
                Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff, streakVelocity, 90, default, Main.rand.NextFloat(0.7f, 1.3f)).noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Covers a client whose timer was just behind the server's kill packet.
            bool reachedBlast = Mode == ModeAttached && ModeTimer >= ChargeTicks - DetonateTolerance;
            if (reachedBlast)
            {
                Detonate();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            // Its target or caster vanished: the spell breaks apart instead of disappearing.
            for (int i = 0; i < 24; i++)
            {
                Vector2 fizzleVelocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff, fizzleVelocity, 100, default, Main.rand.NextFloat(0.7f, 1.2f)).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[0] != 0f)
            {
                return false;
            }

            Texture2D glowTexture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = glowTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float drawScale = 0.8f;
            if (Mode == ModeForming)
            {
                drawScale = 0.8f * MathHelper.Clamp(ModeTimer / (float)AttraidiesArenaActor.CastTicks, 0f, 1f);
            }
            else if (Mode == ModeAttached)
            {
                drawScale = 0.8f + 1.3f * MathHelper.Clamp(ModeTimer / (float)ChargeTicks, 0f, 1f);
            }

            // Alpha 0 makes these colors add to the scene, which gives a glow without restarting the sprite batch.
            Color outerColor = new Color(150, 60, 255, 0);
            Color coreColor = new Color(235, 205, 255, 0);

            for (int i = 0; i < 2; i++)
            {
                float layerRotation = Projectile.rotation + i * MathHelper.PiOver2;
                Main.EntitySpriteDraw(glowTexture, drawPosition, null, outerColor, layerRotation, origin, drawScale, SpriteEffects.None);
                Main.EntitySpriteDraw(glowTexture, drawPosition, null, coreColor, -layerRotation, origin, drawScale * 0.45f, SpriteEffects.None);
            }

            return false;
        }
    }
}
