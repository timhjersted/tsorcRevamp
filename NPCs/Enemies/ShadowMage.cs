using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    class ShadowMage : ModNPC
    {
        int oracleDamage = 15;
        int ice3Damage = 20;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 28;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 450;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 2250; // was 300
            NPC.width = 28;
            NPC.knockBackResist = 0.3f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ShadowMageBanner>();

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1350; 
                NPC.defense = 40;
            }
        }

        // Teleport rhythm, in ticks. The cooldown counts from the last landing. The punish window is the start
        // of it: the mage stands still and cannot teleport, so melee players can close in and finish it.
        const int TeleportCooldownTicks = 290;
        const int LowHealthTeleportCooldownTicks = 210;
        const int PunishWindowTicks = 150;
        const int TeleportWindupTicks = 40;
        const int TeleportRetryTicks = 60;

        // Destination band around the target, in tiles: far enough to play as a caster, close enough to chase down.
        const int TeleportMinDistanceTiles = 14;
        const int TeleportMaxDistanceTiles = 30;
        const int TeleportMinTravelTiles = 5;
        const int TeleportSearchAttempts = 120;
        const float TeleportGiveUpDistance = 3000f;

        // Oracle volley after each landing: a 30-tick tell, then 5 shots 12 ticks apart, only while it can see the target.
        const int OracleVolleyShots = 5;
        const int OracleShotIntervalTicks = 12;
        const int OracleTellTicks = 30;
        const float OracleSpeed = 3f;

        // Icefall: an ice ball forms 8-13 tiles above the target and lingers ~55 ticks before dropping icicles.
        // The ball is offset sideways so it never forms inside the player, and needs a clear drop path down to them.
        const int IceCastIntervalTicks = 150;
        const int IceCastJitterTicks = 60;
        const int IceRetryTicks = 20;
        const float IceDropMinHeight = 128f;
        const float IceDropMaxHeight = 208f;
        const float IceDropMinSideOffset = 12f;
        const float IceDropMaxSideOffset = 40f;
        const int IceDropAttempts = 4;

        // Rare summon of a flanking pair (Cursed Skulls, or Wraiths in Hardmode) 700px either side of the target.
        const int SummonChanceDenominator = 2800;
        const float SummonSideDistance = 700f;

        // Synced through SendExtraAI. ticksSinceTeleport drives the cooldown and the punish window;
        // teleportWindupTimer is 0 when idle and counts up to TeleportWindupTicks during the departure tell.
        int ticksSinceTeleport;
        int teleportWindupTimer;

        // Client-local: the last seen ai[3], so every machine can play the icefall cast effects when the server resets it.
        float previousIceTimer;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ticksSinceTeleport);
            writer.Write(teleportWindupTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ticksSinceTeleport = reader.ReadInt32();
            teleportWindupTimer = reader.ReadInt32();
        }


        #region AI
        // ai[0] = oracle shot timer (negative during the tell), ai[2] = oracle shots fired this volley, ai[3] = icefall timer.
        public override void AI()
        {
            bool isServer = Main.netMode != NetmodeID.MultiplayerClient;

            if (isServer)
            {
                bool targetInvalid = NPC.target < 0 || NPC.target == 255 || !Main.player[NPC.target].active || Main.player[NPC.target].dead;

                if (targetInvalid)
                {
                    NPC.TargetClosest(true);
                    NPC.netUpdate = true;
                }
            }

            Player target = Main.player[NPC.target];

            // Everyone dead: hide off-world and despawn, as the original AI did.
            if (target.dead || !target.active)
            {
                if (isServer)
                {
                    NPC.position = Vector2.Zero;

                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                    }

                    NPC.netUpdate = true;
                }

                return;
            }

            if (target.Center.X > NPC.Center.X)
            {
                NPC.direction = 1;
            }
            else
            {
                NPC.direction = -1;
            }

            NPC.spriteDirection = NPC.direction;

            if (!Main.dedServ)
            {
                if (NPC.life > NPC.lifeMax * 2 / 3)
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y, 200, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                else if (NPC.life <= NPC.lifeMax / 3)
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y, 140, default, 2f);
                    Main.dust[dust].noGravity = true;
                }
            }

            // Arrival burst. ticksSinceTeleport is 0 for exactly one tick after a landing (or a fresh sync of one).
            if (ticksSinceTeleport == 0 && !Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                for (int i = 0; i < 24; i++)
                {
                    Vector2 burstVelocity = Main.rand.NextVector2Circular(5f, 5f);
                    Dust arrivalDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, burstVelocity.X, burstVelocity.Y, 60, default, 1.8f);
                    arrivalDust.noGravity = true;
                }
            }

            ticksSinceTeleport++;


            #region Teleport
            int teleportCooldown = TeleportCooldownTicks;

            if (NPC.life <= NPC.lifeMax / 2)
            {
                teleportCooldown = LowHealthTeleportCooldownTicks;
            }

            bool inPunishWindow = ticksSinceTeleport < PunishWindowTicks;
            bool windingUp = teleportWindupTimer > 0;

            // Planted while punishable or while winding up: zero horizontal speed so it cannot slide away from melee.
            if (inPunishWindow || windingUp)
            {
                NPC.velocity.X = 0f;
            }
            else
            {
                NPC.velocity.X *= 0.85f;
            }

            if (isServer && !windingUp && ticksSinceTeleport >= teleportCooldown)
            {
                teleportWindupTimer = 1;
                NPC.netUpdate = true;
            }
            else if (windingUp)
            {
                teleportWindupTimer++;

                // Departure tell: cursed-flame dust pulled in toward the body, denser as the windup completes.
                if (!Main.dedServ)
                {
                    float windupProgress = teleportWindupTimer / (float)TeleportWindupTicks;
                    int windupDustCount = 1 + (int)(windupProgress * 3f);

                    for (int i = 0; i < windupDustCount; i++)
                    {
                        Vector2 dustOffset = Main.rand.NextVector2CircularEdge(40f, 40f);
                        Vector2 dustPosition = NPC.Center + dustOffset;
                        Vector2 dustVelocity = -dustOffset * 0.08f;
                        Dust windupDust = Dust.NewDustPerfect(dustPosition, DustID.CursedTorch, dustVelocity, 40, default, 1.4f);
                        windupDust.noGravity = true;
                    }
                }

                if (isServer && teleportWindupTimer >= TeleportWindupTicks)
                {
                    teleportWindupTimer = 0;
                    NPC.TargetClosest(true);
                    target = Main.player[NPC.target];

                    // Destination search. A spot must be standable floor with 3x4 tiles of headroom, no lava, inside the
                    // 14-30 tile band around the target, 5+ tiles from where it stood, and in line of sight of the target.
                    bool foundSpot = false;
                    Vector2 destination = Vector2.Zero;
                    float distanceToTarget = Vector2.Distance(NPC.Center, target.Center);
                    int targetTileX = (int)(target.Center.X / 16f);
                    int targetTileY = (int)(target.Bottom.Y / 16f);
                    float minDistance = TeleportMinDistanceTiles * 16f;
                    float maxDistance = TeleportMaxDistanceTiles * 16f;
                    float minTravel = TeleportMinTravelTiles * 16f;

                    for (int attempt = 0; attempt < TeleportSearchAttempts && !foundSpot && distanceToTarget < TeleportGiveUpDistance; attempt++)
                    {
                        int candidateX = targetTileX + Main.rand.Next(-TeleportMaxDistanceTiles, TeleportMaxDistanceTiles + 1);
                        int scanStartY = targetTileY + Main.rand.Next(-TeleportMaxDistanceTiles, TeleportMaxDistanceTiles / 2);

                        if (!WorldGen.InWorld(candidateX, scanStartY, 20))
                        {
                            continue;
                        }

                        // Walk down the column to the first floor tile, judge that one spot, then move to the next attempt.
                        for (int floorY = scanStartY; floorY < scanStartY + 20; floorY++)
                        {
                            Tile floorTile = Framing.GetTileSafely(candidateX, floorY);
                            bool floorIsSolid = floorTile.HasTile && !floorTile.IsActuated && (Main.tileSolid[floorTile.TileType] || Main.tileSolidTop[floorTile.TileType]);

                            if (!floorIsSolid)
                            {
                                continue;
                            }

                            bool headroomClear = !Collision.SolidTiles(candidateX - 1, candidateX + 1, floorY - 4, floorY - 1);
                            Tile feetTile = Framing.GetTileSafely(candidateX, floorY - 1);
                            bool feetInLava = feetTile.LiquidAmount > 0 && feetTile.LiquidType == LiquidID.Lava;

                            Vector2 candidatePosition = new Vector2(candidateX * 16f + 8f - NPC.width / 2f, floorY * 16f - NPC.height);
                            Vector2 candidateCenter = candidatePosition + new Vector2(NPC.width / 2f, NPC.height / 2f);
                            float candidateDistance = Vector2.Distance(candidateCenter, target.Center);
                            bool inDistanceBand = candidateDistance >= minDistance && candidateDistance <= maxDistance;
                            bool farFromOldSpot = Vector2.Distance(candidateCenter, NPC.Center) >= minTravel;

                            bool spotUsable = headroomClear && !feetInLava && inDistanceBand && farFromOldSpot;

                            if (spotUsable)
                            {
                                bool canSeeTarget = Collision.CanHitLine(candidatePosition, NPC.width, NPC.height, target.position, target.width, target.height);

                                if (canSeeTarget)
                                {
                                    foundSpot = true;
                                    destination = candidatePosition;
                                }
                            }

                            break;
                        }
                    }

                    if (foundSpot)
                    {
                        NPC.position = destination;
                        NPC.velocity = Vector2.Zero;
                        ticksSinceTeleport = 0;

                        // Every landing starts a fresh oracle volley behind its tell.
                        NPC.ai[0] = -OracleTellTicks;
                        NPC.ai[2] = 0;
                    }
                    else
                    {
                        // No legal spot: stay here and try again shortly, without re-entering the punish window.
                        ticksSinceTeleport = teleportCooldown - TeleportRetryTicks;
                    }

                    NPC.netUpdate = true;
                }
            }
            #endregion


            bool hasLineOfSight = Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height);
            windingUp = teleportWindupTimer > 0;


            #region Oracle volley
            bool volleyActive = NPC.ai[2] < OracleVolleyShots && !windingUp;

            if (volleyActive)
            {
                NPC.ai[0]++;

                // Tell: green motes gather at the mage for the 30 ticks before the first shot.
                if (NPC.ai[0] < 0 && !Main.dedServ)
                {
                    Vector2 tellOffset = Main.rand.NextVector2CircularEdge(28f, 28f);
                    Dust tellDust = Dust.NewDustPerfect(NPC.Center + tellOffset, DustID.CursedTorch, -tellOffset * 0.1f, 50, Color.Green, 1.5f);
                    tellDust.noGravity = true;
                }

                // A shot waits (timer holds past 12) until the target is in view, so it never fires into a wall.
                if (NPC.ai[0] >= OracleShotIntervalTicks && hasLineOfSight)
                {
                    if (isServer)
                    {
                        Vector2 toTarget = target.Center - NPC.Center;
                        Vector2 shotVelocity = Vector2.Normalize(toTarget) * OracleSpeed;
                        int oracleType = ModContent.ProjectileType<TheOracle>();
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, shotVelocity, oracleType, oracleDamage, 0f, Main.myPlayer);
                    }

                    if (!Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }

                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                }
            }
            #endregion


            #region Icefall
            if (!windingUp)
            {
                NPC.ai[3]++;
            }

            if (isServer && NPC.ai[3] >= IceCastIntervalTicks && hasLineOfSight && !windingUp)
            {
                bool foundDropPoint = false;
                Vector2 dropPoint = Vector2.Zero;

                for (int attempt = 0; attempt < IceDropAttempts && !foundDropPoint; attempt++)
                {
                    float sideOffset = Main.rand.NextFloat(IceDropMinSideOffset, IceDropMaxSideOffset) * Main.rand.NextBool().ToDirectionInt();
                    float dropHeight = Main.rand.NextFloat(IceDropMinHeight, IceDropMaxHeight);
                    Vector2 candidatePoint = new Vector2(target.Center.X + sideOffset, target.Top.Y - dropHeight);

                    bool pointIsOpen = !Collision.SolidCollision(candidatePoint - new Vector2(16f, 16f), 32, 32);
                    bool dropPathClear = Collision.CanHitLine(candidatePoint - new Vector2(8f, 8f), 16, 16, target.position, target.width, target.height);

                    if (pointIsOpen && dropPathClear)
                    {
                        foundDropPoint = true;
                        dropPoint = candidatePoint;
                    }
                }

                if (foundDropPoint)
                {
                    // ai1 = 255 points the ball's "player directly below" early-pop check at the unused dummy player
                    // slot, so it always lingers its full fade before dropping icicles. Velocity only sets the sprite's facing (aiStyle 4 holds still).
                    int iceType = ModContent.ProjectileType<EnemySpellIce3Ball>();
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), dropPoint, new Vector2(0f, 4f), iceType, ice3Damage, 0f, Main.myPlayer, 0f, 255f);
                    NPC.ai[3] = -Main.rand.Next(IceCastJitterTicks);
                }
                else
                {
                    NPC.ai[3] = IceCastIntervalTicks - IceRetryTicks;
                }

                NPC.netUpdate = true;
            }

            // Cast effects on every machine: the icefall timer dropping sharply means the server just cast.
            if (!Main.dedServ && NPC.ai[3] < previousIceTimer - 10f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);

                for (int i = 0; i < 12; i++)
                {
                    Dust castDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height / 2, DustID.IceTorch, 0f, -3f, 40, default, 1.5f);
                    castDust.noGravity = true;
                }
            }

            previousIceTimer = NPC.ai[3];
            #endregion


            #region Summons
            bool rollSummon = isServer && Main.rand.NextBool(SummonChanceDenominator);

            if (rollSummon)
            {
                bool summonWraiths = Main.rand.NextBool();
                bool summonAllowed = !summonWraiths || Main.hardMode;
                int summonType = NPCID.CursedSkull;

                if (summonWraiths)
                {
                    summonType = NPCID.Wraith;
                }

                // One summon on each side of the target, skipping any side whose spawn point is inside tiles.
                for (int side = -1; side <= 1 && summonAllowed; side += 2)
                {
                    Vector2 spawnPoint = new Vector2(target.Center.X + side * SummonSideDistance, target.Center.Y);
                    bool spawnPointBlocked = Collision.SolidCollision(spawnPoint - new Vector2(20f, 24f), 40, 48);

                    if (spawnPointBlocked)
                    {
                        continue;
                    }

                    int summonIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPoint.X, (int)spawnPoint.Y + 24, summonType);

                    if (summonIndex < Main.maxNPCs && Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: summonIndex);
                    }
                }
            }
            #endregion
        }

        #endregion

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D glowTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/ShadowMage_Glowmask");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 56, 54), lightColor, NPC.rotation, new Vector2(28, 27), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 56, 54), lightColor, NPC.rotation, new Vector2(28, 27), NPC.scale, effects, 0f);
            }
        }
        public override void FindFrame(int frameHeight)
        {

            // Casting frame while moving, winding up a teleport, or anywhere in an oracle volley (tell included).
            bool castingVolley = NPC.ai[2] < OracleVolleyShots && (NPC.ai[0] < 0 || NPC.ai[0] >= OracleShotIntervalTicks);
            bool moving = NPC.velocity.X != 0 || NPC.velocity.Y != 0;

            if (moving || castingVolley || teleportWindupTimer > 0)
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            else
            {
                NPC.frame.Y = 0 * frameHeight;
            }
        }

        #region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 100, 1, 1, 40));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 10, 1, 1, 1));

        }
    }
}