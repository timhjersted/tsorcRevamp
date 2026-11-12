using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.NetModules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends
{
    [AutoloadBossHead]
    class WaterFiendKraken : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 110;
            NPC.height = 170;
            DrawOffsetY = 50;
            NPC.damage = trueContactDamage;
            NPC.defense = 35;
            NPC.aiStyle = -1;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 325000;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.rarity = 34;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.WaterFiendKraken.DespawnHandler"), Color.DeepSkyBlue, 180);
            InitializeMoves();
        }


        int hypnoticDisruptorDamage = 35;
        int cursedFlamesDamage = 40;
        int geyserDamage = 30;
        int plasmaOrbDamage = 65;
        int trueContactDamage = 120; //Contact damage does not get multiplied by 4, hence the higher values
        int chargeContactDamage = 200;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        KrakenMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<KrakenMove> MoveList;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveCounter
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public Player Target
        {
            get => Main.player[NPC.target];
        }

        int MoveTimer = 0;
        NPCDespawnHandler despawnHandler;

        public override void AI()
        {
                base.AI();

            if (NPC.life <= 0 && !NPC.active)
            {
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == NPC.whoAmI)
                    {
                        proj.Kill(); 
                    }
                }
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.4f, 0f, 0.25f);

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }
            if (MoveIndex >= MoveList.Count)
            {
                MoveIndex = 0;
            }

            CurrentMove.Move();
        }

        Vector2 chargeVelocity = new Vector2(0, 0);
        float ChargeTimer = 0;
        float projectileTimer = -120;
        float projectileType = 0;
        bool charging = false;
        private void CursedFireSpam()
        {
            ChargeTimer++;
            if (ChargeTimer >= 500)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 29, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 5);
                Main.dust[dust].velocity = UsefulFunctions.Aim(NPC.Center, Main.dust[dust].position, 5);
            }
            if (ChargeTimer == 600)
            {
                chargeVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 19);
                charging = true;
            }
            if (charging)
            {
                NPC.velocity = chargeVelocity;
                NPC.damage = chargeContactDamage;

                //Check if it's passed the player by at least 500 units while charging, and if so stop
                if (Vector2.Distance(NPC.Center, Target.Center) > 500)
                {
                    Vector2 vectorDiff = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1);
                    double angleDiff = UsefulFunctions.CompareAngles(NPC.velocity, vectorDiff);

                    if (angleDiff > MathHelper.Pi / 2)
                    {
                        charging = false;
                        NPC.damage = trueContactDamage;
                        ChargeTimer = 0;
                        MoveCounter++;
                    }
                }
            }

            //Most of its movement is done here
            if (!charging)
            {
                FloatOminouslyTowardPlayer();
            }
            if (MoveCounter >= 3 && projectileTimer == 0)
            {
                NextAttack();
            }

            #region Projectiles and NPCs
            // Runs on every machine so clients get the plasma-orb charge dust and the fire sounds.
            // Spawns and the next-volley roll are server-only; projectileTimer/projectileType are synced in SendExtraAI.
            if (!charging)
            {
                projectileTimer++;
                ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
                if (projectileType == 9 && !Main.dedServ)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Vector2 dir = Main.rand.NextVector2Circular(300, 300);
                        Vector2 dustPos = NPC.Center + dir;
                        Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                        Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.FireworkFountain_Blue, dustVel, Scale: 1);
                        thisDust.noGravity = true;
                        thisDust.shader = dustShader;
                    }
                }
                if (projectileTimer >= 0)
                {
                    float offset = MathHelper.ToRadians(-20 + 10 * projectileTimer);
                    if (projectileType == 9)
                    {
                        offset = MathHelper.ToRadians(-30 + 10 * projectileTimer);
                    }
                    if (projectileType < 6)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 10);
                            projVector = projVector.RotatedBy(offset);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer, 0, NPC.target);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    if (projectileType >= 6 && projectileType != 9)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 5);
                            projVector = projVector.RotatedBy(offset);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, NPC.target, 1f);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    if (projectileType == 9)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 12);
                            projVector = projVector.RotatedBy(offset);
                            projVector += (Main.player[NPC.target].velocity / 2);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    // Volley done after 5 shots (7 for plasma orbs): every machine resets the timer; only the server
                    // rolls the next volley type and pushes it to clients (types 0-5 sharks, 6-8 disrupters, 9 plasma orbs).
                    int lastShotTick = 4;

                    if (projectileType == 9)
                    {
                        lastShotTick = 6;
                    }

                    if (projectileTimer == lastShotTick)
                    {
                        projectileTimer = -120;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            projectileType = Main.rand.Next(10);
                            NPC.netUpdate = true;
                        }
                    }
                }
            }
            #endregion

            //If low on life, start flooding the chamber constantly
            if (NPC.life < (NPC.lifeMax / 2))
            {
                //Don't start a new flood if it's below the normal water line, to avoid fucking with it as much as possible
                //(water-line Y is legacy 2000-space; MapTileY shifts it on the expanded world)
                bool aboveWaterLine = (NPC.Center.Y / 16) < ExpandedWorldTransform.MapTileY(1820, 1713);
                bool inOpenSpace = !UsefulFunctions.IsTileReallySolid(NPC.Center / 16);

                // Only the server decides when a flood starts. Clients learn radius went nonzero via ReceiveExtraAI,
                // then advance it locally each tick so the sweep dust tracks the server's flood front.
                bool isServerOrSingleplayer = Main.netMode != NetmodeID.MultiplayerClient;
                bool startNewFlood = radius == 0 && isServerOrSingleplayer && aboveWaterLine && inOpenSpace;

                if (startNewFlood)
                {
                    NPC.netUpdate = true;
                }

                if (radius != 0 || startNewFlood)
                {
                    radius++;
                    FloodArena();
                }

                if (radius > 300)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projCenter = Target.Center;
                        projCenter += Main.rand.NextVector2CircularEdge(500, 500);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, 0, 0, ModContent.ProjectileType<InkGeyser>(), geyserDamage, 0f, Main.myPlayer, Target.whoAmI);
                    }

                    chamberFlooded = !chamberFlooded;
                    radius = 0;
                    NPC.netUpdate = true;
                }
            }
        }

        // Flood front distance in tiles (one per tick, wraps at 300). Synced: clients draw the sweep dust from it.
        float radius = 0;
        // false = this sweep fills the chamber, true = this sweep drains it. Synced so clients agree on the phase.
        bool chamberFlooded;
        //Rectangle arena = new Rectangle(1557, 1639, 467, 103);
        // Server/singleplayer only: tiles the flood fill writes next tick.
        List<Point> activeTiles;
        int projType = 0;

        // 4-way flood fill neighbours (tile offsets).
        static readonly Point[] FloodNeighbourOffsets = { new Point(0, 1), new Point(1, 0), new Point(-1, 0), new Point(0, -1) };
        private void AquaWave()
        {
            if (NPC.Center != ArenaCenter)
            {
                if (MoveCounter < 45) 
                {
                    MoveCounter++;

                    
                    if (Main.rand.NextBool(2)) 
                    {
                        Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(20, 20);
                        Dust fallingDust = Dust.NewDustPerfect(dustPos, DustID.WaterCandle, new Vector2(0, 2), 100, default, 3f);
                        fallingDust.noGravity = false; 
                        fallingDust.velocity *= 0.3f; 
                    }

                    NPC.velocity = Vector2.Zero;

                    return; 
                }

                for (int i = 0; i < 100; i++) 
                {
                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(100, 100);

                    Dust explosionDust = Dust.NewDustPerfect(dustPos, DustID.WaterCandle, Vector2.Zero, 200, default, 3f); 
                    explosionDust.noGravity = true; 
                    explosionDust.velocity = Main.rand.NextVector2Circular(8, 8);
                    explosionDust.scale = Main.rand.NextFloat(1.5f, 3f); // 
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);

                NPC.Center = ArenaCenter;
                NPC.velocity = Vector2.Zero;

                for (int i = 0; i < 50; i++) 
                {
                    Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(100, 100);
                    Dust arrivalDust = Dust.NewDustPerfect(dustPos, DustID.WaterCandle, Vector2.Zero, 200, default, 2f);
                    arrivalDust.noGravity = true;
                    arrivalDust.velocity = Main.rand.NextVector2Circular(3, 3);
                }

                MoveCounter = 0;

                return;

            NPC.velocity = Vector2.Zero;

        }

            if (projType >= 8)
            {
                ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
                for (int j = 0; j < 10; j++)
                {
                    Vector2 dir = Main.rand.NextVector2Circular(300, 300);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.FireworkFountain_Blue, dustVel, Scale: 1);
                    thisDust.noGravity = true;
                    thisDust.shader = dustShader;
                }
            }

            if (Main.GameUpdateCount % 40 == 0)
            {
                if (projType < 5)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 10);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                if (projType >= 5 && projType < 8)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 5);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, NPC.target, 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                if (projType >= 8)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 15);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                // Server rolls the next shot type and syncs it, so clients' sounds and orb-charge dust match what spawns.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    projType = Main.rand.Next(10);
                    NPC.netUpdate = true;
                }
            }

            radius++;
            FloodArena();

            if (radius > 300)
            {
                chamberFlooded = !chamberFlooded;
                radius = 0;
                NPC.netUpdate = true;

                MoveTimer++;

                if (MoveTimer >= 5)
                {
                    MoveTimer = 0;
                    NextAttack();
                }
            }
        }

        private void FloatOminouslyTowardPlayer()
        {
            Vector2 krakenMaxSpeed = new Vector2(3, 4);
            float krakenAccelerationX = 0.05f;
            float krakenAccelerationY = 0.05f;

            if (NPC.Center.X < Target.Center.X)
            {
                NPC.velocity.X += krakenAccelerationX;
            }
            else
            {
                NPC.velocity.X -= krakenAccelerationX;
            }

            //This is the part that makes it bob up and down as it moves
            //If it's moving up
            if (NPC.velocity.Y < 0)
            {
                //And it's not more than 120 units above the player
                if (Target.Center.Y - NPC.Center.Y <= 120)
                {
                    //Keep moving up
                    NPC.velocity.Y -= krakenAccelerationY;
                }
                //If we are more than 120 units above the player, start accelerating down
                else
                {
                    NPC.velocity.Y += krakenAccelerationY;
                }
            }
            else
            {
                //Do the same thing, but reversed if it's moving down. Could probably simplify this, but this format makes it clear what it's doing.
                if (Target.Center.Y - NPC.Center.Y <= -120)
                {
                    NPC.velocity.Y -= krakenAccelerationY;
                }
                else
                {
                    NPC.velocity.Y += krakenAccelerationY;
                }
            }

            NPC.velocity = Vector2.Clamp(NPC.velocity, -krakenMaxSpeed, krakenMaxSpeed);
        }

        //Legacy 2000-space pixel coord; property so the expanded-world transform applies on every read.
        Vector2 ArenaCenter => ExpandedWorldTransform.MapWorld(new Vector2(1820 * 16, 1702 * 16));

        float cursedRadius = 1400;
        private void CursedBarrage()
        {
            MoveCounter++;
            NPC.velocity = Vector2.Zero;

            cursedRadius = 250 + ((1200 - MoveCounter) * 1.5f);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active && NPC.Distance(Main.player[i].Center) > cursedRadius)
                {
                    Main.player[i].AddBuff(BuffID.Blackout, 5);
                    Main.player[i].AddBuff(BuffID.Venom, 5);
                }
            }

            for (int j = 0; j < 100; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(cursedRadius, cursedRadius);
                Vector2 dustPos = NPC.Center + dir;
                Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 3);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }

            for (int j = 0; j < 10; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(130, 130);
                Vector2 dustPos = NPC.Center + dir;
                Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation());
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 2);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }

            int cursedFlameCooldown = 60;
            if (chamberFlooded)
            {
                cursedFlameCooldown = 80;
            }
            if (MoveCounter % cursedFlameCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 projCenter = Main.rand.NextVector2CircularEdge(cursedRadius, cursedRadius) + NPC.Center;
                Vector2 projVector = UsefulFunctions.Aim(projCenter, Main.player[NPC.target].Center, 10);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, projVector.X, projVector.Y, ModContent.ProjectileType<EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer, 1, NPC.target);
            }

            int waterJetCooldown = 160;
            if (MoveCounter % waterJetCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 projCenter = Target.Center;
                projCenter += Main.rand.NextVector2CircularEdge(500, 500);

                Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, 0, 0, ModContent.ProjectileType<InkGeyser>(), geyserDamage, 0f, Main.myPlayer, Target.whoAmI);
            }

            if (MoveCounter > 1200)
            {
                NextAttack();
            }
        }
        private void FloodArena()
        {

            //Don't flood anything if outside of adventure mode or far from the arena center
            //(Y bounds are legacy 2000-space and get mapped on the expanded world; X bounds are invariant)
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode || NPC.Center.Y < ExpandedWorldTransform.MapTileY(1820, 1660) * 16 || NPC.Center.Y > ExpandedWorldTransform.MapTileY(1820, 1744) * 16 || NPC.Center.X < 1560 * 16 || NPC.Center.X > 2011 * 16)
            {
                return;
            }

            bool isServerOrSingleplayer = Main.netMode != NetmodeID.MultiplayerClient;

            //First tick of a sweep: seed the flood front at the Kraken's tile. Only the server writes tiles.
            if (radius == 1)
            {
                if (isServerOrSingleplayer)
                {
                    activeTiles = new List<Point>();
                    activeTiles.Add(NPC.Center.ToTileCoordinates());
                }

                return;
            }

            // Flood fill, server/singleplayer only: set every front tile full (or empty when draining), then queue its
            // open neighbours that aren't at that level yet as next tick's front. Changed tiles are synced in one batch.
            if (isServerOrSingleplayer && activeTiles != null)
            {
                byte liquidLevel = 255;

                if (chamberFlooded)
                {
                    liquidLevel = 0;
                }

                List<Point> nextTiles = new List<Point>();
                HashSet<Point> queuedTiles = new HashSet<Point>();
                HashSet<int> changedTiles = new HashSet<int>();

                for (int i = 0; i < activeTiles.Count; i++)
                {
                    Point tileCoords = activeTiles[i];

                    // Capture this area's pre-fight water before the first write lands in it, so it can be put back.
                    KrakenArenaFloodRestore.SnapshotAround(tileCoords.X, tileCoords.Y);

                    // Type set explicitly: a tile's leftover liquid type must not turn the flood into lava/honey.
                    Tile tile = Main.tile[tileCoords.X, tileCoords.Y];
                    tile.LiquidType = LiquidID.Water;
                    tile.LiquidAmount = liquidLevel;

                    // Packed the way vanilla's liquid net module expects: X in the high 16 bits, Y in the low 16.
                    int packedCoords = (tileCoords.X << 16) | tileCoords.Y;
                    changedTiles.Add(packedCoords);

                    for (int j = 0; j < FloodNeighbourOffsets.Length; j++)
                    {
                        int neighbourX = tileCoords.X + FloodNeighbourOffsets[j].X;
                        int neighbourY = tileCoords.Y + FloodNeighbourOffsets[j].Y;

                        if (!WorldGen.InWorld(neighbourX, neighbourY, 10))
                        {
                            continue;
                        }

                        bool alreadyAtLevel = Main.tile[neighbourX, neighbourY].LiquidAmount == liquidLevel;
                        bool isSolid = UsefulFunctions.IsTileReallySolid(neighbourX, neighbourY);

                        if (alreadyAtLevel || isSolid)
                        {
                            continue;
                        }

                        Point neighbour = new Point(neighbourX, neighbourY);

                        if (queuedTiles.Add(neighbour))
                        {
                            nextTiles.Add(neighbour);
                        }
                    }
                }

                activeTiles = nextTiles;
                KrakenArenaFloodRestore.SendLiquidChanges(changedTiles);
            }

            if (Main.dedServ)
            {
                return;
            }

            // Sweep dust: four mirrored streams moving at the flood front (radius tiles out from the Kraken).
            // Velocity ramps up over the first 20 ticks of the sweep. Drawn on every client from the synced radius.
            ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.CyanGradientDye), Main.LocalPlayer);
            Vector2[] mirrorSigns = { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) };

            for (int i = 0; i < 90; i++)
            {
                float offset = Main.rand.NextFloat(-720, 0);
                Vector2 velocity = new Vector2(-16, 0);

                if (radius < 20)
                {
                    velocity *= radius / 20;
                }

                Vector2 positionOffset = Vector2.Zero;
                positionOffset.X += radius * 16;
                positionOffset.X += offset;
                positionOffset.Y += offset;

                for (int j = 0; j < mirrorSigns.Length; j++)
                {
                    Vector2 dustPosition = NPC.Center + positionOffset * mirrorSigns[j];

                    if (UsefulFunctions.IsTileReallySolid(dustPosition / 16))
                    {
                        continue;
                    }

                    Dust sweepDust = Dust.NewDustPerfect(dustPosition, DustID.InfernoFork, velocity, 10, default, 2);
                    sweepDust.noGravity = true;
                    sweepDust.shader = shader;
                }
            }
        }
        // Server/singleplayer only: MoveIndex/MoveCounter are ai[0]/ai[1], so clients follow through the NPC sync
        // instead of switching moves on their own (possibly out-of-phase) counters.
        private void NextAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.netUpdate = true;
            MoveIndex++;
            if (MoveIndex > MoveList.Count)
            {
                MoveIndex = 0;
            }

            MoveCounter = 0;
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<KrakenMove> {
                new KrakenMove(CursedFireSpam, KrakenAttackID.CursedFireSpam, "Cursed Fire"),
                new KrakenMove(AquaWave, KrakenAttackID.AquaWave, "Aqua Wave"),
                new KrakenMove(CursedBarrage, KrakenAttackID.CursedBarrage, "Cursed Barrage"),
                };
        }

        private class KrakenAttackID
        {
            public const short CursedFireSpam = 0;
            public const short AquaWave = 1;
            public const short CursedBarrage = 2;
        }
        private class KrakenMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public KrakenMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }

        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 5.0)
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

        // Flood phase sync: the server writes the tiles; clients only need radius (sweep dust) and which way the sweep goes.
        // Plus the server-rolled shot types and the counters that gate them, so client dust/sounds match what spawns.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(radius);
            writer.Write(chamberFlooded);
            writer.Write(MoveTimer);
            writer.Write(projectileTimer);
            writer.Write(projectileType);
            writer.Write(projType);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            radius = reader.ReadSingle();
            chamberFlooded = reader.ReadBoolean();
            MoveTimer = reader.ReadInt32();
            projectileTimer = reader.ReadSingle();
            projectileType = reader.ReadSingle();
            projType = reader.ReadInt32();
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<KrakenBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarrowBlade>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<KrakenFlesh>(), 1, 3, 3));
            npcLoot.Add(notExpertCondition);
        }
        public override void OnKill()
        {
                base.OnKill();

                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == NPC.whoAmI) 
                    {
                        proj.Kill();
                    }
                }
                
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 8").Type, 1f);
            }
        }
    }

    /// <summary>
    /// Puts the Kraken chamber's water back after the fight. Nothing in code defines the chamber's water: its resting
    /// state is the map as built (a standing pool below the legacy Y 1713 "normal water line"). So before the flood
    /// first writes in an area, the server snapshots the liquid of the surrounding 3x3 block of 50x50-tile cells.
    /// When no Kraken is active (killed, despawned, or despawned after a wipe) or no player is alive, every changed
    /// tile is restored and synced.
    /// Known gap: water that flows 50+ tiles out of every snapshotted cell is not removed.
    /// Known gap: a dedicated server shut down mid-fight saves the flood (singleplayer restores on quit).
    /// </summary>
    public class KrakenArenaFloodRestore : ModSystem
    {
        private const int CellSize = 50;

        // NetLiquidModule packets carry a ushort count at 6 bytes per tile; stay well under the 65535-byte packet limit.
        private const int MaxTilesPerLiquidPacket = 8000;

        private static readonly Dictionary<Point, LiquidSnapshot> snapshots = new Dictionary<Point, LiquidSnapshot>();

        // Cells whose whole 3x3 neighbourhood is already snapshotted, so the per-tile check is one hash lookup.
        private static readonly HashSet<Point> coveredCells = new HashSet<Point>();

        private class LiquidSnapshot
        {
            public readonly byte[] Amounts = new byte[CellSize * CellSize];
            public readonly byte[] Types = new byte[CellSize * CellSize];
        }

        /// <summary>Server/SP: call before writing liquid at this tile. Snapshots any of the 3x3 cells around it not yet captured.</summary>
        public static void SnapshotAround(int tileX, int tileY)
        {
            Point centerCell = new Point(tileX / CellSize, tileY / CellSize);

            if (!coveredCells.Add(centerCell))
            {
                return;
            }

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    Point cell = new Point(centerCell.X + offsetX, centerCell.Y + offsetY);

                    if (snapshots.ContainsKey(cell))
                    {
                        continue;
                    }

                    LiquidSnapshot snapshot = new LiquidSnapshot();
                    int originX = cell.X * CellSize;
                    int originY = cell.Y * CellSize;

                    for (int localX = 0; localX < CellSize; localX++)
                    {
                        for (int localY = 0; localY < CellSize; localY++)
                        {
                            int snapshotTileX = originX + localX;
                            int snapshotTileY = originY + localY;

                            if (!WorldGen.InWorld(snapshotTileX, snapshotTileY))
                            {
                                continue;
                            }

                            Tile tile = Main.tile[snapshotTileX, snapshotTileY];
                            int index = localX * CellSize + localY;
                            snapshot.Amounts[index] = tile.LiquidAmount;
                            snapshot.Types[index] = (byte)tile.LiquidType;
                        }
                    }

                    snapshots.Add(cell, snapshot);
                }
            }
        }

        /// <summary>
        /// Server only (no-op elsewhere): sends liquid changes to clients through vanilla's NetLiquidModule, the same
        /// path the liquid sim uses. Coords are packed (X &lt;&lt; 16) | Y. Split into batches to bound packet size.
        /// </summary>
        public static void SendLiquidChanges(HashSet<int> packedTileCoords)
        {
            if (Main.netMode != NetmodeID.Server || packedTileCoords.Count == 0)
            {
                return;
            }

            HashSet<int> batch = new HashSet<int>();

            foreach (int packedCoords in packedTileCoords)
            {
                batch.Add(packedCoords);

                if (batch.Count >= MaxTilesPerLiquidPacket)
                {
                    NetLiquidModule.CreateAndBroadcastByChunk(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                NetLiquidModule.CreateAndBroadcastByChunk(batch);
            }
        }

        public override void PostUpdateNPCs()
        {
            // Server/SP only: clients receive the restored tiles through SendLiquidChanges.
            if (Main.netMode == NetmodeID.MultiplayerClient || snapshots.Count == 0)
            {
                return;
            }

            bool krakenActive = NPC.AnyNPCs(ModContent.NPCType<WaterFiendKraken>());
            bool anyPlayerAlive = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }

            // Fight over: killed, despawned, or a wipe. Put the chamber back.
            if (!krakenActive || !anyPlayerAlive)
            {
                RestoreSnapshots();
            }
        }

        // Writes back every tile whose liquid differs from its snapshot, reframes it so the liquid sim settles it, and syncs those tiles.
        private static void RestoreSnapshots()
        {
            HashSet<int> changedTiles = new HashSet<int>();

            foreach (KeyValuePair<Point, LiquidSnapshot> entry in snapshots)
            {
                int originX = entry.Key.X * CellSize;
                int originY = entry.Key.Y * CellSize;
                LiquidSnapshot snapshot = entry.Value;

                for (int localX = 0; localX < CellSize; localX++)
                {
                    for (int localY = 0; localY < CellSize; localY++)
                    {
                        int tileX = originX + localX;
                        int tileY = originY + localY;

                        if (!WorldGen.InWorld(tileX, tileY))
                        {
                            continue;
                        }

                        Tile tile = Main.tile[tileX, tileY];
                        int index = localX * CellSize + localY;
                        byte savedAmount = snapshot.Amounts[index];
                        byte savedType = snapshot.Types[index];

                        bool unchanged = tile.LiquidAmount == savedAmount && (savedAmount == 0 || tile.LiquidType == savedType);

                        if (unchanged)
                        {
                            continue;
                        }

                        tile.LiquidAmount = savedAmount;
                        tile.LiquidType = savedType;
                        WorldGen.SquareTileFrame(tileX, tileY);

                        int packedCoords = (tileX << 16) | tileY;
                        changedTiles.Add(packedCoords);
                    }
                }
            }

            snapshots.Clear();
            coveredCells.Clear();
            SendLiquidChanges(changedTiles);
        }

        // Singleplayer quit mid-fight: restore before the world is written to disk.
        public override void PreSaveAndQuit()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                RestoreSnapshots();
            }
        }

        public override void OnWorldUnload()
        {
            snapshots.Clear();
            coveredCells.Clear();
        }
    }
}