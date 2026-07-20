using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs
{
    // The combat→movement hand-off contract (Phase 2). Shared by the combat layer (tsorcRevampAIs.RunFighterCombat*)
    // and BOTH movement substrates — LocalMover (here) and SmartFighter4AI.Run (wired in Step 4).
    //   SeizesBody    — combat/teleport owns velocity this frame → the mover should no-op those frames.
    //   HoldForAttack — stop-and-fire: the mover should pin position / hold its standoff so combat can shoot.
    // Populated by the combat layer and obeyed by the movement layer.
    // See Documentation/CombatMovementSeparation_Plan.md.
    internal struct FighterCombatIntent
    {
        public bool SeizesBody;
        public bool HoldForAttack;
    }

    /// <summary>
    /// The cheap, reactive movement substrate for FighterAI enemies — extracted verbatim from BasicAI (Phase 2
    /// Step 2, pure refactor). Walks toward the target, faces it, brakes, accelerates, height-aware wall-climbs,
    /// gap-jumps, breaks doors, drops through platforms, auto-steps small rises, double-jumps, and runs the
    /// position-immobility anti-stuck. It has NO pathfinding (that's the SmartFighter4AI substrate) — this is what
    /// walks enemies off cliffs, and the dumb default selected when NavSearchRadius == 0 (dispatch added in Step 4).
    /// </summary>
    public static class LocalMover
    {
        /// <summary>
        /// Runs one frame of reactive local movement. Returns the post-movement line of sight to the target
        /// (tile state may have changed mid-frame via a platform drop), which the caller feeds to its debug log
        /// and combat triggers.
        /// </summary>
        // internal (not public) because it takes the internal FighterCombatIntent — and it's only ever called
        // from within the mod (BasicAI now; the NavSearchRadius dispatch in Step 4). Avoids CS0051.
        internal static bool Run(NPC npc, tsorcRevampGlobalNPC globalNPC, float topSpeed, float acceleration,
                                 float brakingPower, bool isArcher, int doorBreakingDamage, bool fleeing,
                                 bool lineOfSight, ref FighterCombatIntent intent)
        {
            // `lineOfSight` is the PRE-movement LOS the caller (BasicAI) already computed (and used to decide
            // intent.HoldForAttack). It is refreshed near the end of this method — tile state can change
            // mid-frame via a platform drop — and the refreshed value is returned.

            if (intent.SeizesBody)
            {
                npc.velocity *= 0.1f;
                if (npc.velocity.LengthSquared() < 0.01f)
                {
                    npc.velocity = Vector2.Zero;
                }
                return lineOfSight;
            }

            // Face the player (dead zone avoids jitter when nearly aligned). Fleeing walks away.
            if (Math.Abs(Main.player[npc.target].Center.X - npc.Center.X) > 30)
            {
                npc.direction = Main.player[npc.target].Center.X <= npc.Center.X ? -1 : 1;
                if (fleeing) // walk away from the player
                {
                    npc.direction *= -1;
                }
                npc.spriteDirection = npc.direction;

                if (globalNPC.CanWalkBackwards && !fleeing)
                {
                    float playerDist = npc.Distance(Main.player[npc.target].Center);
                    if (playerDist < 180f)
                    {
                        // Reverse physical direction to walk backwards
                        npc.direction *= -1;
                        // Keep sprite facing the player
                        npc.spriteDirection = Main.player[npc.target].Center.X <= npc.Center.X ? -1 : 1;
                    }
                }
            }

            //If moving more than max speed, then slow down
            if (globalNPC.PounceCooldown <= 240)
            {
                if (npc.velocity.X > topSpeed)
                {
                    npc.velocity.X -= brakingPower;
                    if (npc.velocity.X < 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
                if (npc.velocity.X < -topSpeed)
                {
                    npc.velocity.X += brakingPower;
                    if (npc.velocity.X > 0)
                    {
                        npc.velocity.X = 0;
                    }
                }
            }

            // Stop-to-fire EXECUTION (Step 3). The DECISION now lives in the combat layer
            // (tsorcRevampAIs.SenseHoldForAttack), which set intent.HoldForAttack before this mover ran — and
            // also ticked the post-attack pause. Here the mover only OBEYS: pin the body so combat can fire.
            // SF4 will honor the same flag in Step 4. (The archer-aim hold is still gated separately in the
            // acceleration block below — folding it into HoldForAttack is a Step-4 refinement.)
            if (intent.HoldForAttack)
            {
                if (globalNPC.FighterRangedStandShotsRemaining > 0)
                    npc.velocity.X = 0f; // hard stop: hold position while firing a burst
                else
                    npc.velocity.X *= 0.8f; // gradual stop: post-attack breather
            }

            //Accelerate in the direction they are facing (unless the npc is an aiming archer or holding to fire)
            if ((!isArcher || globalNPC.ArcherAimDirection == 0) && !intent.HoldForAttack)
            {
                if (npc.velocity.X < topSpeed && npc.direction == 1)
                {
                    npc.velocity.X += acceleration;
                    if (npc.velocity.X > topSpeed)
                    {
                        npc.velocity.X = topSpeed;
                    }
                }
                else
                {
                    if (npc.velocity.X > -topSpeed && npc.direction == -1)
                    {
                        npc.velocity.X -= acceleration;
                        if (npc.velocity.X < -topSpeed)
                        {
                            npc.velocity.X = -topSpeed;
                        }
                    }
                }
            }


            // Ledge-halt: optionally stop before a significant drop when we already have LOS.
            // Only halts if dropping would put the NPC meaningfully lower than the player —
            // tiny drops and same-elevation crossings are left alone so jump/gap logic can handle them.
            // A hard cap of 180 frames prevents indefinite ledge-camping.
            if (!globalNPC.CanPassThroughWalls && globalNPC.HaltAtLedge && lineOfSight && npc.velocity.Y == 0f && !globalNPC.Fleeing)
            {
                int aheadX = npc.direction == -1
                    ? (int)(npc.position.X / 16f) - 1
                    : (int)((npc.position.X + npc.width) / 16f);
                int belowY = (int)(npc.position.Y + npc.height + 8f) / 16;

                if (!UsefulFunctions.IsTileReallySolid(aheadX, belowY))
                {
                    // Scan downward for solid ground (ignores water/air). Cap at 10 tiles.
                    int dropDepth = 10;
                    for (int dy = 1; dy <= 10; dy++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(aheadX, belowY + dy))
                        {
                            dropDepth = dy;
                            break;
                        }
                    }

                    // Where would we land, in world-space Y pixels?
                    float landingWorldY = (belowY + dropDepth) * 16f;
                    float playerWorldY = Main.player[npc.target].Center.Y;

                    // Halt only if landing would put us more than 3 tiles below the player
                    // (losing elevation), AND the drop is at least 4 tiles deep (not a tiny step).
                    bool wouldLoseElevation = landingWorldY > playerWorldY + 48f;
                    bool dropIsSignificant = dropDepth >= 4;
                    bool shouldHalt = wouldLoseElevation && dropIsSignificant && globalNPC.LedgeHaltTimer < 180;

                    if (shouldHalt)
                    {
                        npc.velocity.X = 0f;
                        globalNPC.LedgeHaltTimer++;
                    }
                    else
                    {
                        globalNPC.LedgeHaltTimer = 0;
                    }
                }
                else
                {
                    globalNPC.LedgeHaltTimer = 0;
                }
            }
            else
            {
                globalNPC.LedgeHaltTimer = 0;
            }

            //Jumping and platform falling code, copied and edited from Firebomb Hollow
            int x_in_front;
            if (npc.direction == -1)
            {
                x_in_front = (int)(npc.position.X / 16f) - 1;
            }
            else
            {
                x_in_front = (int)((npc.position.X + npc.width) / 16f);
            }

            int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
            //Dust.DrawDebugBox(new Rectangle(x_in_front * 16, y_above_feet * 16, 16, 16));
            int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
            bool standing_on_solid_tile = false;

            //Check if standing on a solid tile
            int x_left_edge = (int)npc.position.X / 16;
            int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (UsefulFunctions.IsTileReallySolid(l, y_below_feet)) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                    }
                }
            }

            // Flat-ground navigation check for large enemies (MinSurfaceWidth > 0).
            if (globalNPC.RequiresFlatGround)
            {
                int minWidth = globalNPC.MinSurfaceWidth;
                int centerTileX = (int)(npc.Center.X / 16f);

                bool currentValid = UsefulFunctions.IsPartOfValidSurface(centerTileX, y_below_feet, minWidth);
                bool targetValid = UsefulFunctions.IsPartOfValidSurface(x_in_front, y_below_feet, minWidth)
                                || UsefulFunctions.IsPartOfValidSurface(x_in_front, y_below_feet - 1, minWidth);

                if (!currentValid || !targetValid)
                {
                    if (standing_on_solid_tile)
                    {
                        npc.velocity.Y = -globalNPC.MaxJumpPower * 0.8f;
                        npc.velocity.X = globalNPC.MaxJumpBoost * npc.direction;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        if (!targetValid)
                        {
                            npc.velocity.X *= 0.8f;
                        }
                    }
                }
            }

            // Terrain handling: height-aware wall climbs (scaled to MaxJumpPower) + gap jump + door breaking.
            if (standing_on_solid_tile)
            {
                if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1))
                {
                    // Step 4a — height-aware climb scaled to THIS enemy's jump power. Sense the full wall
                    // height ahead, work out how high its own MaxJumpPower (default 8f = vanilla) can actually
                    // reach given gravity, and jump only walls within that reach AND with clearance above the
                    // top to land on (headroom). Walls taller than it can clear, or capped by a ceiling, are
                    // NOT jumped — it stands pressed and the position-immobility anti-stuck below disengages it
                    // (→ Patrol/teleport) instead of bouncing forever (see the RedKnight wall-bounce log).
                    // This also wires the per-enemy MaxJumpPower / MaxJumpBoost levers into the dumb (tier-0)
                    // path for the first time (they were tier-≥1-only before).
                    float jumpPower = globalNPC.MaxJumpPower;
                    float grav = npc.gravity > 0.01f ? npc.gravity : 0.3f;
                    bool wallAhead = UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1)
                                  || UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 2);
                    if (wallAhead)
                    {
                        // Contiguous wall height ahead (solid tiles stacked up from the step row).
                        int wallTiles = 0;
                        while (wallTiles < 12 && UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - wallTiles))
                            wallTiles++;
                        int topY = y_above_feet - wallTiles; // first open row above the wall

                        // ~2 tiles of headroom above the wall top at both the wall and the landing column.
                        bool headroom = !UsefulFunctions.IsTileReallySolid(x_in_front, topY)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front, topY - 1)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, topY)
                                     && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, topY - 1);

                        // Apex height (tiles) this jump can reach; 0.9 margin so it clears the corner, not just
                        // grazes the apex.
                        int reachableTiles = (int)((jumpPower * jumpPower) / (2f * grav) / 16f * 0.9f);

                        if (wallTiles >= 2 && wallTiles <= reachableTiles && headroom)
                        {
                            // Jump just hard enough to clear (wallTiles + 1) tiles, capped at MaxJumpPower.
                            float neededV = (float)Math.Sqrt(2f * grav * (wallTiles + 1) * 16f);
                            npc.velocity.Y = -Math.Min(neededV, jumpPower);
                            npc.netUpdate = true;
                        }
                        // wallTiles == 1 -> AutoStepUp glide handles it; too tall / capped / no headroom -> no
                        // jump, stand and give up.
                    }
                    // (1-tile step removed: the smooth AutoStepUp glide below replaces the old -5f hop so
                    //  tier-0 walkers no longer visibly bounce over every single-tile bump.)
                    else if (npc.directionY < 0 && !UsefulFunctions.IsTileReallySolid(x_in_front, y_below_feet) && !UsefulFunctions.IsTileReallySolid(x_in_front + npc.direction, y_below_feet))
                    {
                        // Cross a gap toward an above player — vertical from MaxJumpPower, horizontal from MaxJumpBoost.
                        npc.velocity.Y = -jumpPower;
                        npc.velocity.X += globalNPC.MaxJumpBoost * npc.direction;
                        npc.netUpdate = true;
                    }

                    if (UsefulFunctions.IsTileReallySolid(x_in_front, y_above_feet - 1) && Main.tile[x_in_front, y_above_feet - 1].TileType == 10 && (doorBreakingDamage > 0))
                    {
                        npc.velocity.Y = 0;
                        if (Main.GameUpdateCount % 60 == 0)
                        {
                            npc.velocity.X = 0.5f * -npc.direction;
                            globalNPC.DoorBreakProgress += doorBreakingDamage;
                            WorldGen.KillTile(x_in_front, y_above_feet - 1, true, true, false);
                            if (globalNPC.DoorBreakProgress >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                globalNPC.DoorBreakProgress = 0;
                                if (!WorldGen.OpenDoor(x_in_front, y_above_feet, npc.direction))
                                {
                                    // Door is jammed (e.g. blocked above): stop ramming. The position-immobility
                                    // anti-stuck disengages it to Patrol/teleport after ~1.5s.
                                    npc.velocity.X = 0;
                                }
                                else if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, (float)x_in_front, (float)y_above_feet, (float)npc.direction, 0);
                                }
                            }
                        }
                    }
                }
            }



            //Can fall through platforms
            bool standing_on_platforms = true;
            bool atLeastOnePlatform = false;
            if (npc.velocity.Y == 0)
            {
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (TileID.Sets.Platforms[Main.tile[l, y_below_feet].TileType])
                    {
                        atLeastOnePlatform = true;
                    }
                    else
                    {
                        if (Main.tile[l, y_below_feet].HasTile)
                        {
                            standing_on_platforms = false;
                        }
                    }
                }
            }

            // Drop through platforms when player is below.
            // Threshold is 64px (4 tiles) to avoid accidental drops on tiny height differences.
            // Gate on low horizontal speed: noTileCollide disables ALL tile collision, so a fast-moving
            // NPC would clip through walls. Only drop when nearly stopped horizontally.
            bool playerIsBelow = Main.player[npc.target].Center.Y > npc.Center.Y + 32f;
            // Drop when the player is below AND either roughly overhead, or we've failed to make progress for
            // ~1s (DisengageTimer) — the FSM-clean replacement for the old "BoredTimer > 60" fallback.
            bool shouldDropPlatform = playerIsBelow && (globalNPC.DisengageTimer > 60 || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < 300);

            if (standing_on_platforms && atLeastOnePlatform && shouldDropPlatform)
            {
                npc.noTileCollide = true;
            }

            // Smoothly glide up 1-tile steps instead of the removed -5f hop. Skip while platform-dropping
            // (noTileCollide) so the lift doesn't fight the drop.
            if (!npc.noTileCollide && !npc.noGravity)
            {
                tsorcRevampAIs.AutoStepUp(npc);
            }

            // Reset double jump when standing (velocity.Y == 0)
            if (npc.velocity.Y == 0f)
            {
                globalNPC.UsedDoubleJump = false;
            }

            // Double jump: apex-triggered mid-air second jump for capable enemies.
            // Gated purely by the CanDoubleJump bool (default false) — tier-independent.
            if (globalNPC.CanDoubleJump && !globalNPC.UsedDoubleJump)
            {
                // Fire when clearly falling (player is still above us) — velocity.Y > 1.5f avoids
                // triggering on the first few frames after stepping off a ledge
                if (!standing_on_solid_tile && npc.velocity.Y > 1.5f && npc.directionY < 0)
                {
                    npc.velocity.Y = -globalNPC.DoubleJumpPower;
                    globalNPC.UsedDoubleJump = true;
                    npc.netUpdate = true;
                }
            }

            // Refresh after movement phase — tile state may have changed (platform drop, etc.)
            lineOfSight = Main.player[npc.target].CanHit(npc);

            // Step 4a LOS fix: boredom is now "no LOS" only — the old `playerOnDifferentLevel`
            // (|Δy| > 48f) qualifier is dropped, since with the FSM a vertical offset no longer means
            // "can't reach" (the give-up clock + anti-stuck handle genuinely unreachable players).

            // ── Tier-0 position-immobility anti-stuck (Step 4a) ──────────────────────────────────────
            // A visible-but-walled player must not trap the NPC forever, so this fires regardless of LOS.
            // It is keyed on the NPC's OWN position (not velocity / distance), so the repeated wall-jump
            // bounce — which keeps velocity.Y != 0 and makes the player distance oscillate — can no longer
            // hide the stuck state (see the RedKnight wall-bounce log). If the NPC hasn't moved ~2 tiles for
            // ~1.5s while unable to engage, hand off to the FSM give-up (→ Search/Patrol/teleport). Adjacent
            // melee and aiming archers reset the timer so legitimate "stand and fight" doesn't read as stuck.
            if (globalNPC.PursuitState != PursuitState.Patrol)
            {
                bool canEngage = lineOfSight && npc.Distance(Main.player[npc.target].Center) < 64f;
                bool aiming = globalNPC.ArcherAimDirection != 0f || globalNPC.FighterRangedStandShotsRemaining > 0;
                if (canEngage || aiming || npc.Distance(globalNPC.StuckCheckPos) > 32f)
                {
                    globalNPC.StuckCheckPos = npc.Center;
                    globalNPC.StuckTimer = 0;
                }
                else
                {
                    globalNPC.StuckTimer++;
                    if (globalNPC.StuckTimer > 90) // ~1.5s without moving 2 tiles and unable to engage
                    {
                        globalNPC.StuckTimer = 0;
                        globalNPC.StuckCheckPos = npc.Center;
                        NavBehavior.ForceDisengage(npc, globalNPC);
                    }
                }
            }

            return lineOfSight;
        }
    }
}
