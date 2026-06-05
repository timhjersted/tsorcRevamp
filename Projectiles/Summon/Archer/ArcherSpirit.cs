using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    public class ArcherSpirit : ModProjectile
    {
        public int lifetime = 10800; // 3 minutes in ticks
        public int shootCooldown = 0;
        public int stuckTimer = 0;
        public int patrolTimer = 0;
        public int patrolDirection = 0;
        public int lastTargetWhoAmI = -1;
        public bool isAiming = false;
        public bool isBackingAway = false;
        public float walkFrameCounter = 0f;
        public bool isReturning = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 20;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 48;
            Projectile.tileCollide = true;
            Projectile.minion = true;
            Projectile.scale = 1.0f;
            Projectile.minionSlots = 2f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!CheckActive(player))
            {
                return;
            }

            lifetime--;
            if (lifetime <= 0)
            {
                Projectile.Kill();
                return;
            }

            // Capture actual physics velocity from last frame before AI updates it
            float oldVelocityX = Projectile.velocity.X;

            // Fall through platforms if the player is below us (default idle/return behavior)
            Projectile.shouldFallThrough = player.position.Y + player.height > Projectile.position.Y + Projectile.height;

            // Emit yellow light
            Lighting.AddLight(Projectile.Center, 0.8f, 0.75f, 0.4f);

            float distToPlayer = Vector2.Distance(Projectile.Center, player.Center);
            if (isReturning)
            {
                if (distToPlayer <= 160f)
                {
                    isReturning = false;
                }
            }
            else
            {
                if (distToPlayer > 320f)
                {
                    isReturning = true;
                }
            }
            bool returningToPlayer = isReturning;
            bool isTryingToMove = false;

            if (returningToPlayer)
            {
                isAiming = false;
                isBackingAway = false;
                isTryingToMove = true;

                bool navigating = SmartProjectileFighterAI.Run(Projectile, player, topSpeed: 2.4f, acceleration: 0.1f, navSearchRadius: 24, gravity: 0.4f);
                if (!navigating)
                {
                    // Move towards player horizontally (slower returning speed: 2.4f)
                    float horizDistToPlayer = Math.Abs(player.Center.X - Projectile.Center.X);
                    if (horizDistToPlayer > 12f)
                    {
                        int dir = player.Center.X > Projectile.Center.X ? 1 : -1;
                        Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, dir * 2.4f, 0.1f);
                        Projectile.spriteDirection = dir;
                    }
                    else
                    {
                        Projectile.velocity.X *= 0.8f;
                    }

                    if (TileCollisionInFront())
                    {
                        Projectile.velocity.Y = -7f;
                    }
                }

                // Animation
                if (Projectile.velocity.Y != 0f)
                {
                    Projectile.frame = 0; // jump frame is 0
                }
                else
                {
                    // Walk frames are 6 to the end
                    if (Projectile.frame < 6 || Projectile.frame >= Main.projFrames[Projectile.type])
                    {
                        Projectile.frame = 6;
                        walkFrameCounter = 0f;
                    }
                    float speed = Math.Abs(Projectile.velocity.X);
                    walkFrameCounter += Math.Max(0.5f, speed) * 2.8f;
                    if (walkFrameCounter >= 6f)
                    {
                        walkFrameCounter = 0f;
                        Projectile.frame++;
                        if (Projectile.frame >= Main.projFrames[Projectile.type])
                        {
                            Projectile.frame = 6;
                        }
                    }
                }
            }
            else
            {
                // Targeting and attacking logic
                bool foundTarget = false;
                Vector2 targetCenter = Vector2.Zero;
                float distToTarget = 800f;
                NPC targetNPC = null;

                // 1. Minion target feature check (whip targets)
                NPC minionTarget = Projectile.OwnerMinionAttackTargetNPC;
                if (minionTarget != null && minionTarget.CanBeChasedBy(this))
                {
                    float between = Vector2.Distance(minionTarget.Center, Projectile.Center);
                    if (between < 800f)
                    {
                        targetCenter = minionTarget.Center;
                        distToTarget = between;
                        foundTarget = true;
                        targetNPC = minionTarget;
                    }
                }

                // 2. Check if previous target is still valid (Stickiness to avoid flipping jitter)
                if (!foundTarget && lastTargetWhoAmI >= 0 && lastTargetWhoAmI < Main.maxNPCs)
                {
                    NPC prevTarget = Main.npc[lastTargetWhoAmI];
                    if (prevTarget.active && prevTarget.CanBeChasedBy(this))
                    {
                        float horizDistToPlayer = Math.Abs(prevTarget.Center.X - player.Center.X);
                        float vertDistToPlayer = Math.Abs(prevTarget.Center.Y - player.Center.Y);
                        if (horizDistToPlayer < 480f && vertDistToPlayer < 480f) // within 30 tiles
                        {
                            float between = Vector2.Distance(prevTarget.Center, Projectile.Center);
                            if (between < 800f)
                            {
                                targetCenter = prevTarget.Center;
                                distToTarget = between;
                                foundTarget = true;
                                targetNPC = prevTarget;
                            }
                        }
                    }
                }

                // 3. Scan for a new target if none found or previous is invalid
                if (!foundTarget)
                {
                    int maxLife = -1;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.CanBeChasedBy(this))
                        {
                            float horizDistToPlayer = Math.Abs(npc.Center.X - player.Center.X);
                            float vertDistToPlayer = Math.Abs(npc.Center.Y - player.Center.Y);
                            
                            // More aggressive default search range: within 30 tiles of player
                            if (horizDistToPlayer < 480f && vertDistToPlayer < 480f)
                            {
                                float between = Vector2.Distance(npc.Center, Projectile.Center);
                                if (npc.life > maxLife)
                                {
                                    maxLife = npc.life;
                                    targetCenter = npc.Center;
                                    distToTarget = between;
                                    foundTarget = true;
                                    targetNPC = npc;
                                }
                            }
                        }
                    }
                }

                // Update sticky target tracker
                if (foundTarget && targetNPC != null)
                {
                    lastTargetWhoAmI = targetNPC.whoAmI;
                    // Fall through platforms if the target is below us
                    Projectile.shouldFallThrough = targetNPC.position.Y + targetNPC.height > Projectile.position.Y + Projectile.height;
                }
                else
                {
                    lastTargetWhoAmI = -1;
                }

                if (foundTarget && targetNPC != null)
                {
                    int targetDir = targetCenter.X > Projectile.Center.X ? 1 : -1;
                    
                    // Deadzone of 16 pixels to prevent rapid left-right wiggling/flipping when directly overlapping horizontally
                    float horizDist = Math.Abs(targetCenter.X - Projectile.Center.X);
                    if (horizDist > 16f)
                    {
                        Projectile.spriteDirection = targetDir;
                    }

                    bool canSee = Collision.CanHitLine(Projectile.Center, 1, 1, targetCenter, 1, 1);

                    // Update aiming and backing-away states with hysteresis
                    if (!canSee)
                    {
                        isAiming = false;
                        isBackingAway = false;
                    }
                    else
                    {
                        // Hysteresis for aiming
                        if (isAiming)
                        {
                            if (distToTarget > 750f)
                            {
                                isAiming = false;
                            }
                        }
                        else
                        {
                            if (distToTarget <= 650f)
                            {
                                isAiming = true;
                            }
                        }

                        // Hysteresis for backing away
                        if (isBackingAway)
                        {
                            if (distToTarget > 220f)
                            {
                                isBackingAway = false;
                            }
                        }
                        else
                        {
                            if (distToTarget < 140f)
                            {
                                isBackingAway = true;
                            }
                        }
                    }

                    if (!isAiming)
                    {
                        isTryingToMove = true;

                        bool navigating = SmartProjectileFighterAI.Run(Projectile, targetNPC, topSpeed: 2.4f, acceleration: 0.1f, navSearchRadius: 24, gravity: 0.4f);
                        if (!navigating)
                        {
                            // Walk towards target to close distance (slower walk speed: 2.4f)
                            float horizDistToTarget = Math.Abs(targetCenter.X - Projectile.Center.X);
                            if (horizDistToTarget > 12f)
                            {
                                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, targetDir * 2.4f, 0.1f);
                            }
                            else
                            {
                                Projectile.velocity.X *= 0.8f;
                            }
                            if (TileCollisionInFront())
                            {
                                Projectile.velocity.Y = -7f;
                            }
                        }

                        // Animation
                        if (Projectile.velocity.Y != 0f)
                        {
                            Projectile.frame = 0; // jump frame
                        }
                        else
                        {
                            if (Projectile.frame < 6 || Projectile.frame >= Main.projFrames[Projectile.type])
                            {
                                Projectile.frame = 6;
                                walkFrameCounter = 0f;
                            }
                            float speed = Math.Abs(Projectile.velocity.X);
                            walkFrameCounter += Math.Max(0.5f, speed) * 2.8f;
                            if (walkFrameCounter >= 6f)
                            {
                                walkFrameCounter = 0f;
                                Projectile.frame++;
                                if (Projectile.frame >= Main.projFrames[Projectile.type])
                                {
                                    Projectile.frame = 6;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Shoot arrow (Always attempt to shoot when isAiming is true, even while backing away)
                        if (shootCooldown > 0)
                        {
                            shootCooldown--;
                        }

                        if (shootCooldown <= 0)
                        {
                            shootCooldown = 80; // reset cooldown

                            if (Main.myPlayer == Projectile.owner)
                            {
                                Vector2 shootVel = UsefulFunctions.BallisticTrajectory(Projectile.Center, targetCenter, 13f, 0.035f);
                                if (shootVel == Vector2.Zero)
                                {
                                    shootVel = UsefulFunctions.Aim(Projectile.Center, targetCenter, 13f);
                                }

                                // Nerfed damage to max mana / 4
                                int damage = player.statManaMax2 / 4;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center.X,
                                    Projectile.Center.Y,
                                    shootVel.X,
                                    shootVel.Y,
                                    ModContent.ProjectileType<ArcherSpiritArrow>(),
                                    damage,
                                    2f,
                                    Projectile.owner
                                );
                            }

                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, Projectile.Center);
                        }

                        if (isBackingAway)
                        {
                            // Back away from target, but only if we stay within 20 tiles of player
                            int backDir = -targetDir;
                            float nextDistToPlayer = Vector2.Distance(Projectile.Center + new Vector2(backDir * 4f, 0f), player.Center);
                            
                            if (nextDistToPlayer < 320f)
                            {
                                isTryingToMove = true;

                                // Back away speed (slower: 1.2f)
                                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, backDir * 1.2f, 0.1f);
                                if (TileCollisionInFront())
                                {
                                    Projectile.velocity.Y = -7f;
                                }

                                // Animation
                                if (Projectile.velocity.Y != 0f)
                                {
                                    Projectile.frame = 0; // jump frame
                                }
                                else
                                {
                                    if (Projectile.frame < 6 || Projectile.frame >= Main.projFrames[Projectile.type])
                                    {
                                        Projectile.frame = 6;
                                        walkFrameCounter = 0f;
                                    }
                                    float speed = Math.Abs(Projectile.velocity.X);
                                    walkFrameCounter += Math.Max(0.5f, speed) * 2.8f;
                                    if (walkFrameCounter >= 6f)
                                    {
                                        walkFrameCounter = 0f;
                                        Projectile.frame++;
                                        if (Projectile.frame >= Main.projFrames[Projectile.type])
                                        {
                                            Projectile.frame = 6;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Can't back away further without leaving player range, stand still
                                Projectile.velocity.X *= 0.8f;
                                
                                // Aiming animation
                                int aimFrame = 3;
                                Vector2 aimVector = targetCenter - Projectile.Center;
                                if (Math.Abs(aimVector.Y) > Math.Abs(aimVector.X) * 2f)
                                {
                                    aimFrame = aimVector.Y > 0f ? 1 : 5;
                                }
                                else if (Math.Abs(aimVector.X) > Math.Abs(aimVector.Y) * 2f)
                                {
                                    aimFrame = 3;
                                }
                                else
                                {
                                    aimFrame = aimVector.Y > 0f ? 2 : 4;
                                }
                                Projectile.frame = aimFrame;
                            }
                        }
                        else
                        {
                            // In shooting range, not backing away: plant feet
                            Projectile.velocity.X *= 0.8f;

                            // Aiming animation: angle calculation (0 jump, 1 down, 2 slight down/forward, 3 straight forward, 4 slight up, 5 up)
                            int aimFrame = 3;
                            Vector2 aimVector = targetCenter - Projectile.Center;
                            if (Math.Abs(aimVector.Y) > Math.Abs(aimVector.X) * 2f)
                            {
                                aimFrame = aimVector.Y > 0f ? 1 : 5;
                            }
                            else if (Math.Abs(aimVector.X) > Math.Abs(aimVector.Y) * 2f)
                            {
                                aimFrame = 3;
                            }
                            else
                            {
                                aimFrame = aimVector.Y > 0f ? 2 : 4;
                            }
                            Projectile.frame = aimFrame;
                        }
                    }
                }
                else
                {
                    isAiming = false;
                    isBackingAway = false;

                    // Idle behavior: random pacing / patrol within 20 tiles of player
                    patrolTimer--;
                    if (patrolTimer <= 0)
                    {
                        int r = Main.rand.Next(3);
                        if (r == 0)
                        {
                            patrolDirection = 0;
                            patrolTimer = Main.rand.Next(60, 180); // Idle for 1-3 seconds
                        }
                        else
                        {
                            patrolDirection = r == 1 ? -1 : 1;
                            patrolTimer = Main.rand.Next(60, 120); // Move for 1-2 seconds
                        }
                    }

                    float dx = Projectile.Center.X - player.Center.X;
                    if (patrolDirection == 1 && dx > 320f)
                    {
                        patrolDirection = -1;
                    }
                    else if (patrolDirection == -1 && dx < -320f)
                    {
                        patrolDirection = 1;
                    }

                    if (patrolDirection == 0)
                    {
                        Projectile.velocity.X *= 0.8f;
                        if (Projectile.velocity.Y != 0f)
                        {
                            Projectile.frame = 0; // jump frame
                        }
                        else
                        {
                            Projectile.frame = 6; // standing idle walk frame
                        }
                    }
                    else
                    {
                        isTryingToMove = true;

                        // Patrol speed (slower: 1.2f)
                        Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, patrolDirection * 1.2f, 0.1f);
                        Projectile.spriteDirection = patrolDirection;

                        if (TileCollisionInFront())
                        {
                            Projectile.velocity.Y = -7f;
                        }

                        if (Projectile.velocity.Y != 0f)
                        {
                            Projectile.frame = 0; // jump frame
                        }
                        else
                        {
                            if (Projectile.frame < 6 || Projectile.frame >= Main.projFrames[Projectile.type])
                            {
                                Projectile.frame = 6;
                                walkFrameCounter = 0f;
                            }
                            float speed = Math.Abs(Projectile.velocity.X);
                            walkFrameCounter += Math.Max(0.5f, speed) * 2.8f;
                            if (walkFrameCounter >= 6f)
                            {
                                walkFrameCounter = 0f;
                                Projectile.frame++;
                                if (Projectile.frame >= Main.projFrames[Projectile.type])
                                {
                                    Projectile.frame = 6;
                                }
                            }
                        }
                    }
                }
            }

            // Stuck logic: Check oldVelocityX from the start of the frame so that
            // velocity adjustments made during AI() do not reset the stuckTimer.
            if (isTryingToMove && Math.Abs(oldVelocityX) < 0.1f)
            {
                stuckTimer++;
            }
            else
            {
                stuckTimer = 0;
            }

            // Debug logging (every 60 frames)
            if (Main.GameUpdateCount % 60 == 0)
            {
                string logPath = System.IO.Path.Combine(Main.SavePath, "Logs", "archer-spirit-bell.log");
                try
                {
                    string logMessage = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] ID: {Projectile.whoAmI}, isAiming: {isAiming}, isBackingAway: {isBackingAway}, stuckTimer: {stuckTimer}, pos: {Projectile.position}, vel: {Projectile.velocity}, oldVelX: {oldVelocityX}, targetID: {lastTargetWhoAmI}, returning: {returningToPlayer}, tryingToMove: {isTryingToMove}\r\n";
                    System.IO.File.AppendAllText(logPath, logMessage);
                }
                catch (System.Exception ex)
                {
                    Mod.Logger.Warn("Failed to write to archer-spirit-bell.log: " + ex.Message);
                }
            }

            if (stuckTimer >= 90) // 1.5 seconds
            {
                stuckTimer = 0;

                // Exiting location dusts
                for (int d = 0; d < 20; d++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 150, default, 1.2f);
                }

                Vector2 oldPos = Projectile.position;

                // Teleport to floor position lookup (within 5-15 tiles of player)
                Vector2 newPos = FindTeleportPosition(player);
                Projectile.position = newPos;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;

                // Arriving location dusts
                for (int d = 0; d < 20; d++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 150, default, 1.2f);
                }

                // Sound effects at old and new pos
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, oldPos);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

            // Apply gravity at the very end of the AI loop unless disabled (e.g. during rope climb)
            if (!SmartProjectileFighterAI.IsGravityDisabled(Projectile))
            {
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
            }
        }

        private Vector2 FindTeleportPosition(Player player)
        {
            for (int attempts = 0; attempts < 30; attempts++)
            {
                int tileOffset = Main.rand.Next(5, 16);
                int side = Main.rand.NextBool() ? -1 : 1;
                int targetTileX = (int)(player.Center.X / 16f) + (tileOffset * side);
                int startTileY = (int)(player.Bottom.Y / 16f);

                // Scan around the player's height level
                for (int yOffset = -5; yOffset <= 5; yOffset++)
                {
                    int testY = startTileY + yOffset;
                    if (WorldGen.InWorld(targetTileX, testY))
                    {
                        Tile floorTile = Main.tile[targetTileX, testY];
                        if (floorTile.HasTile && Main.tileSolid[floorTile.TileType])
                        {
                            Tile space1 = Main.tile[targetTileX, testY - 1];
                            Tile space2 = Main.tile[targetTileX, testY - 2];
                            Tile space3 = Main.tile[targetTileX, testY - 3];

                            bool space1Empty = !space1.HasTile || !Main.tileSolid[space1.TileType];
                            bool space2Empty = !space2.HasTile || !Main.tileSolid[space2.TileType];
                            bool space3Empty = !space3.HasTile || !Main.tileSolid[space3.TileType];

                            if (space1Empty && space2Empty && space3Empty)
                            {
                                return new Vector2(targetTileX * 16f + 8f - Projectile.width / 2f, testY * 16f - Projectile.height);
                            }
                        }
                    }
                }
            }
            return player.Center - new Vector2(Projectile.width / 2f, Projectile.height);
        }

        private bool TileCollisionInFront()
        {
            if (Projectile.velocity.Y != 0f)
            {
                return false;
            }
            // Check tile in front at feet/bottom level (height of block is 16 pixels, check 8 pixels up)
            int checkX = Projectile.spriteDirection < 0 ? -12 : 12;
            Vector2 checkPos = Projectile.Bottom + new Vector2(checkX, -8f);
            Point tilePos = checkPos.ToTileCoordinates();
            return WorldGen.InWorld(tilePos.X, tilePos.Y) && Main.tile[tilePos.X, tilePos.Y].HasTile && Main.tileSolid[Main.tile[tilePos.X, tilePos.Y].TileType];
        }

        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<ArcherSpiritBuff>());
                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<ArcherSpiritBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            float opacity = 1f;
            if (lifetime < 120)
            {
                opacity = lifetime / 120f;
            }

            // Light yellow tint, no transparency (except when fading out at the end of life)
            Color drawColor = lightColor;
            drawColor.R = (byte)Math.Min(255, drawColor.R * 1.2f);
            drawColor.G = (byte)Math.Min(255, drawColor.G * 1.2f);
            drawColor.B = (byte)Math.Min(255, drawColor.B * 0.8f);
            drawColor *= opacity;
            drawColor.A = (byte)(255 * opacity);

            SpriteEffects spriteEffects = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                spriteEffects,
                0
            );

            return false;
        }
    }
}
