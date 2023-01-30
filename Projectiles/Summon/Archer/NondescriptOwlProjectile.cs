using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Archer {
    public class NondescriptOwlProjectile : ModProjectile {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Owl Archer");
            Main.projFrames[Projectile.type] = 33;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            //Projectile.minion = true;
            Projectile.ContinuouslyUpdateDamage = true;
            Projectile.minionSlots = 0;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            DrawOffsetX = -22;
            DrawOriginOffsetY = -41;
        }

        const float SCALING_PER_SLOT = 0.55f;
        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.NondescriptOwlBuff>())) {
                Projectile.timeLeft = 2;
            }

            StatModifier summon = player.GetTotalDamage(DamageClass.Summon);
            int damage = (int)summon.ApplyTo(Projectile.originalDamage);
            int tokenCount = player.ownedProjectileCounts[ModContent.ProjectileType<ArcherToken>()];

            float finalDamage = damage * (1.0f + (SCALING_PER_SLOT * (tokenCount - 1)));

            Projectile.damage = (int)finalDamage;

            Lighting.AddLight(Projectile.Center, 0.25f, 0.25f, 0.25f);
            AI_026();
            Main.NewText("State: " + animationState);
            Main.NewText("Direction: " + Projectile.direction);
            Projectile.spriteDirection = Projectile.direction;

        }
        private float AI_State {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float AI_Timer {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private float AI_InCombatTimer {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private enum AI_States {
            Combat = 0,
            ChaseOwner = 1
        }
        private enum AnimationStates {
            Idle,
            Running,
            Shooting,
            Flying
        }

        AnimationStates animationState = AnimationStates.Idle;
        const int ANIM_CHANGE_REQUESTED = -1;
        const int ANIM_RUN_BEGIN = 1;
        const int ANIM_RUN_FRAMES = 6;
        const int ANIM_SHOOT_BEGIN = 7;
        const int ANIM_SHOOT_FRAMES = 14;
        const int ANIM_SHOOT_SPECIAL_ARROW_RELEASE = 19;
        const int ANIM_FLY_BEGIN = 21;
        const int ANIM_FLY_FRAMES = 12;
        private void ChangeAnimationState(AnimationStates state) {
            if (animationState != state) {
                animationState = state;
                Projectile.frameCounter = 0;
                Projectile.frame = -1; //yes, really.
            }

        }
        bool shouldFaceLeft = false;
        bool shouldFaceRight = false;
        private void AI_026() {
            Player player = Main.player[Projectile.owner];
            if (!Main.player[Projectile.owner].active) {
                Projectile.active = false;
                return;
            }
            shouldFaceLeft = (shouldFaceLeft && animationState == AnimationStates.Shooting);
            bool abovePlayer = false;
            bool tryJump = false;

            bool standingStill = Projectile.position.X - Projectile.oldPosition.X == 0f;

            int spacer = 10;
            int minionOffset = 40 * player.direction;
            if (animationState != AnimationStates.Shooting) {
                if (player.position.X + player.width / 2 < Projectile.position.X + Projectile.width / 2 - spacer + minionOffset) {
                    shouldFaceLeft = true;
                }
                else if (player.position.X + player.width / 2 > Projectile.position.X + Projectile.width / 2 + spacer + minionOffset) {
                    shouldFaceRight = true;
                } 
            }
            if (player.velocity.X == 0) {
                shouldFaceLeft = false;
                shouldFaceRight = false;
            }
            bool attackReady = AI_Timer == 0f;

            if (attackReady) {
                int leashRange = 500;

                if (AI_InCombatTimer > 0f) {
                    leashRange += 250;
                }

                if (player.rocketDelay2 > 0) {
                    AI_State = (float)AI_States.ChaseOwner;
                    ChangeAnimationState(AnimationStates.Flying);
                }
                float toOwnerX = player.position.X + player.width / 2 - Projectile.Center.X;
                float toOwnerY = player.position.Y + player.height / 2 - Projectile.Center.Y;
                float toOwnerDist = (float)Math.Sqrt(toOwnerX * toOwnerX + toOwnerY * toOwnerY);

                if (toOwnerDist > 2000f) {
                    Projectile.position.X = player.position.X + player.width / 2 - Projectile.width / 2;
                    Projectile.position.Y = player.position.Y + player.height / 2 - Projectile.height / 2;
                }
                else if (toOwnerDist > leashRange || Math.Abs(toOwnerY) > 300f && !(AI_InCombatTimer > 0f)) {
                    if (Projectile.type != 324) {
                        if (toOwnerY > 0f && Projectile.velocity.Y < 0f) {
                            Projectile.velocity.Y = 0f;
                        }
                        if (toOwnerY < 0f && Projectile.velocity.Y > 0f) {
                            Projectile.velocity.Y = 0f;
                        }
                    }
                    AI_State = (float)AI_States.ChaseOwner;
                    ChangeAnimationState(AnimationStates.Flying);
                }

            }
            switch (AI_State) {
                case (float)AI_States.Combat: {

                    float offset = Projectile.width * Projectile.minionPos;
                    int attackRate = 30;
                    int minCombatTime = 60;
                    AI_InCombatTimer -= 1f;
                    if (AI_InCombatTimer < 0f) {
                        AI_InCombatTimer = 0f;
                    }
                    int targetWhoAmI = -1;
                    bool skippedAttack = false;
                    if (AI_Timer > 0f) {
                        AI_Timer -= 1f;
                    }
                    else {
                        float currentClosestNPC_X = Projectile.position.X;
                        float posY = Projectile.position.Y;
                        float currentClosestNPCDist = 100000f;
                        float targetDist = currentClosestNPCDist;

                        NPC forceTargetNPC = Projectile.OwnerMinionAttackTargetNPC;
                        if (forceTargetNPC != null && forceTargetNPC.CanBeChasedBy(this)) {
                            float forceTargetPosX = forceTargetNPC.position.X + forceTargetNPC.width / 2;
                            float forceTargetPosY = forceTargetNPC.position.Y + forceTargetNPC.height / 2;
                            float forceTargetDist = Math.Abs(Projectile.position.X + Projectile.width / 2 - forceTargetPosX) + Math.Abs(Projectile.position.Y + Projectile.height / 2 - forceTargetPosY);
                            if (forceTargetDist < currentClosestNPCDist) {
                                if (targetWhoAmI == -1 && forceTargetDist <= targetDist) {
                                    targetDist = forceTargetDist;
                                    currentClosestNPC_X = forceTargetPosX;
                                    posY = forceTargetPosY;
                                }
                                if (Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, forceTargetNPC.position, forceTargetNPC.width, forceTargetNPC.height)) {
                                    currentClosestNPCDist = forceTargetDist;
                                    currentClosestNPC_X = forceTargetPosX;
                                    posY = forceTargetPosY;
                                    targetWhoAmI = forceTargetNPC.whoAmI;
                                }
                            }
                        }
                        if (targetWhoAmI == -1) {
                            for (int i = 0; i < 200; i++) {
                                if (!Main.npc[i].CanBeChasedBy(this)) {
                                    continue;
                                }
                                float npcPosX = Main.npc[i].position.X + Main.npc[i].width / 2;
                                float npcPosY = Main.npc[i].position.Y + Main.npc[i].height / 2;
                                float npcDist = Math.Abs(Projectile.position.X + Projectile.width / 2 - npcPosX) + Math.Abs(Projectile.position.Y + Projectile.height / 2 - npcPosY);
                                if (npcDist < currentClosestNPCDist) {
                                    if (targetWhoAmI == -1 && npcDist <= targetDist) {
                                        targetDist = npcDist;
                                        currentClosestNPC_X = npcPosX;
                                        posY = npcPosY;
                                    }
                                    if (Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, Main.npc[i].position, Main.npc[i].width, Main.npc[i].height)) {
                                        currentClosestNPCDist = npcDist;
                                        currentClosestNPC_X = npcPosX;
                                        posY = npcPosY;
                                        targetWhoAmI = i;
                                    }
                                }
                            }
                        }
                        if (targetWhoAmI == -1 && targetDist < currentClosestNPCDist) {
                            currentClosestNPCDist = targetDist;
                        }
                        float attackRange = 750f;
                        float targettingRange = 1200;
                        if (targetWhoAmI >= 0 && currentClosestNPCDist < targettingRange + offset) {
                            AI_InCombatTimer = minCombatTime;
                            float toTargetDist = currentClosestNPC_X - (Projectile.position.X + Projectile.width / 2);
                            if (toTargetDist > attackRange || toTargetDist < -attackRange) {
                                if (toTargetDist < -50f) {
                                    shouldFaceLeft = true;
                                    shouldFaceRight = false;
                                }
                                else if (toTargetDist > 50f) {
                                    shouldFaceRight = true;
                                    shouldFaceLeft = false;
                                }

                            }
                            
                            else  {
                                ChangeAnimationState(AnimationStates.Shooting);
                                if (Projectile.owner == Main.myPlayer && Projectile.frame == ANIM_SHOOT_SPECIAL_ARROW_RELEASE - 2) {
                                    AI_Timer = attackRate;
                                    Vector2 aim = UsefulFunctions.GenerateTargetingVector(Projectile.Center, new(currentClosestNPC_X, posY), 12);
                                    int projectileType = ModContent.ProjectileType<OwlsArrow>();
                                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aim, projectileType, Projectile.damage, Projectile.knockBack, Main.myPlayer);
                                    Main.projectile[p].timeLeft = 300;
                                    Main.projectile[p].originalDamage = Projectile.damage;
                                    Projectile.netUpdate = true;
                                    if (aim.X > 0) {
                                        shouldFaceLeft = false;
                                        shouldFaceRight = true;
                                    }
                                    else {
                                        shouldFaceLeft = true;
                                        shouldFaceRight = false;
                                    }
                                }
                            }
                        }
                        else { skippedAttack = true; }
                    }


                    if (AI_Timer != 0f && animationState != AnimationStates.Shooting) {
                        shouldFaceLeft = false;
                        shouldFaceRight = false;
                    }
                    else if (AI_InCombatTimer == 0f) {
                        Projectile.direction = player.direction;
                    }

                    Projectile.rotation = 0f;

                    Projectile.tileCollide = true;

                    float combatChaseTopSpeed = 6f;
                    float combatChaseAccel = 0.2f;
                    if (combatChaseTopSpeed < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y)) {
                        combatChaseTopSpeed = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                        combatChaseAccel = 0.3f;
                    }


                    if (shouldFaceLeft && attackReady) {
                        if (Projectile.velocity.X > -3.5) {
                            Projectile.velocity.X -= combatChaseAccel;
                        }
                        else {
                            Projectile.velocity.X -= combatChaseAccel * 0.25f;
                        }
                        if (Projectile.velocity.X > -0.5f && ((animationState != AnimationStates.Running) && (animationState != AnimationStates.Shooting))) {
                            ChangeAnimationState(AnimationStates.Running);
                        }
                    }
                    else if (shouldFaceRight && attackReady) {
                        if (Projectile.velocity.X < 0.5f && ((animationState != AnimationStates.Running) && (animationState != AnimationStates.Shooting))) {
                            ChangeAnimationState(AnimationStates.Running);
                        }
                        if (Projectile.velocity.X < 3.5) {
                            Projectile.velocity.X += combatChaseAccel;
                        }
                        else {
                            Projectile.velocity.X += combatChaseAccel * 0.25f;
                        }
                    }
                    else {
                        Projectile.velocity.X *= 0.9f;
                        if (Projectile.velocity.X >= 0f - combatChaseAccel && Projectile.velocity.X <= combatChaseAccel) {
                            Projectile.velocity.X = 0f;
                        }
                    }
                    if (Projectile.velocity.Y > 0)
                        ChangeAnimationState(AnimationStates.Flying);
                    if (shouldFaceLeft || shouldFaceRight) {
                        int tilePosX = (int)(Projectile.position.X + Projectile.width / 2) / 16;
                        int tilePosY = (int)(Projectile.position.Y + Projectile.height / 2) / 16 - 1;
                        if (shouldFaceLeft) {
                            tilePosX -= 2; //2 because its wider than one tile and thus checks from its right edge because of rounding
                        }
                        if (shouldFaceRight) {
                            tilePosX++; //1 is fine here
                        }
                        tilePosX += (int)Projectile.velocity.X;
                        if (WorldGen.SolidTile(tilePosX, tilePosY)) {
                            tryJump = true;
                        }
                    }
                    if (player.position.Y + player.height - 8f > Projectile.position.Y + Projectile.height) {
                        abovePlayer = true;
                    }
                    Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
                    if (Projectile.velocity.Y == 0f) {
                        if (!abovePlayer && (Projectile.velocity.X < 0f || Projectile.velocity.X > 0f)) {
                            int projGroundTileX = (int)(Projectile.position.X + Projectile.width / 2) / 16;
                            int projGroundTileY = (int)(Projectile.position.Y + Projectile.height / 2) / 16 + 1;
                            if (shouldFaceLeft) {
                                projGroundTileX--;
                            }
                            if (shouldFaceRight) {
                                projGroundTileX++;
                            }
                            //why is this here? this function is static, doesnt take any
                            //parameters as reference, and nothing is done with the return?
                            //it literally does nothing by being here! but im gonna leave it
                            //just in case! because you never know with this game!
                            WorldGen.SolidTile(projGroundTileX, projGroundTileY);
                        }
                        if (tryJump) {
                            int tilePosX = (int)(Projectile.position.X + Projectile.width / 2) / 16;
                            int tilePosY = (int)(Projectile.position.Y + Projectile.height) / 16;
                            if (shouldFaceLeft) {
                                tilePosX -= 2; //see above
                            }
                            if (shouldFaceRight) {
                                tilePosX++;
                            }
                            tilePosX += (int)Projectile.velocity.X;
                            if (WorldGen.SolidTileAllowBottomSlope(tilePosX, tilePosY) || Main.tile[tilePosX, tilePosY].IsHalfBlock || Main.tile[tilePosX, tilePosY].Slope > 0) {
                                {
                                    try {
                                        if (!WorldGen.SolidTile(tilePosX, tilePosY - 1) && !WorldGen.SolidTile(tilePosX, tilePosY - 2)) {
                                            Projectile.velocity.Y = -5.1f;
                                        }
                                        else if (!WorldGen.SolidTile(tilePosX, tilePosY - 2)) {
                                            Projectile.velocity.Y = -7.1f;
                                        }
                                        else if (WorldGen.SolidTile(tilePosX, tilePosY - 5)) {
                                            Projectile.velocity.Y = -11.1f;
                                        }
                                        else if (WorldGen.SolidTile(tilePosX, tilePosY - 4)) {
                                            Projectile.velocity.Y = -10.1f;
                                        }
                                        else {
                                            Projectile.velocity.Y = -9.1f;
                                        }
                                    }
                                    catch {
                                        Projectile.velocity.Y = -9.1f;
                                    }
                                }
                            }
                        }
                    }
                    if (animationState != AnimationStates.Shooting) {
                        if (Projectile.velocity.X > combatChaseTopSpeed) {
                            Projectile.velocity.X = combatChaseTopSpeed;
                        }
                        if (Projectile.velocity.X < 0f - combatChaseTopSpeed) {
                            Projectile.velocity.X = 0f - combatChaseTopSpeed;
                        }
                        if (Projectile.velocity.X < 0f) {
                            Projectile.direction = -1;
                        }
                        if (Projectile.velocity.X > 0f) {
                            Projectile.direction = 1;
                        }
                        if (Projectile.velocity.X > combatChaseAccel && shouldFaceRight) {
                            Projectile.direction = 1;
                        }
                        if (Projectile.velocity.X < 0f - combatChaseAccel && shouldFaceLeft) {
                            Projectile.direction = -1;
                        } 
                    }
                    

                    if ((animationState == AnimationStates.Shooting && skippedAttack) || standingStill && animationState != AnimationStates.Shooting) {
                        ChangeAnimationState(AnimationStates.Idle);
                    }

                    Projectile.velocity.Y += 0.4f;
                    if (Projectile.velocity.Y > 10f) {
                        Projectile.velocity.Y = 10f;
                    }

                    if (animationState == AnimationStates.Shooting) {
                        Projectile.velocity.X *= 0.01f;
                        Projectile.direction = shouldFaceLeft ? -1 : 1;
                        
                    }
                    break;
                }
                case (float)AI_States.ChaseOwner: {

                    int nearOwnerRange = 100;


                    Projectile.tileCollide = false;
                    float toPlayerXDist = player.position.X + player.width / 2 - Projectile.Center.X;

                    toPlayerXDist -= 40 * player.direction;
                    float targettingRange = 800f;

                    bool foundTarget = false;
                    int targetWhoAmI = -1;
                    for (int i = 0; i < 200; i++) {
                        if (!Main.npc[i].CanBeChasedBy(this)) {
                            continue;
                        }
                        float targetXPos = Main.npc[i].position.X + Main.npc[i].width / 2;
                        float targetYPos = Main.npc[i].position.Y + Main.npc[i].height / 2;
                        if (Math.Abs(player.position.X + player.width / 2 - targetXPos) + Math.Abs(player.position.Y + player.height / 2 - targetYPos) < targettingRange) {
                            if (Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, Main.npc[i].position, Main.npc[i].width, Main.npc[i].height)) {
                                targetWhoAmI = i;
                            }
                            foundTarget = true;
                            break;
                        }
                    }
                    if (!foundTarget) {
                        toPlayerXDist -= 40 * Projectile.minionPos * player.direction;
                    }
                    if (foundTarget && targetWhoAmI >= 0) {
                        AI_State = (float)AI_States.Combat;
                    }

                    float toPlayerYDist = player.position.Y + player.height / 2 - Projectile.Center.Y;

                    float toPlayerDistAbs = (float)Math.Sqrt(toPlayerXDist * toPlayerXDist + toPlayerYDist * toPlayerYDist);

                    float idleChaseAccel = 0.4f;
                    float idleChaseTopSpeed = 12f;
                    if (idleChaseTopSpeed < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y)) {
                        idleChaseTopSpeed = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                    }


                    if (toPlayerDistAbs < nearOwnerRange && player.velocity.Y == 0f && Projectile.position.Y + Projectile.height <= player.position.Y + player.height && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                        AI_State = (float)AI_States.Combat;
                        if (Projectile.velocity.Y < -6f) {
                            Projectile.velocity.Y = -6f;
                        }
                    }
                    if (toPlayerDistAbs < 60f) {
                        toPlayerXDist = Projectile.velocity.X;
                        toPlayerYDist = Projectile.velocity.Y;
                    }
                    else {
                        toPlayerDistAbs = idleChaseTopSpeed / toPlayerDistAbs;
                        toPlayerXDist *= toPlayerDistAbs;
                        toPlayerYDist *= toPlayerDistAbs;
                    }

                    if (Projectile.velocity.X < toPlayerXDist) {
                        Projectile.velocity.X += idleChaseAccel;
                        if (Projectile.velocity.X < 0f) {
                            Projectile.velocity.X += idleChaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.X > toPlayerXDist) {
                        Projectile.velocity.X -= idleChaseAccel;
                        if (Projectile.velocity.X > 0f) {
                            Projectile.velocity.X -= idleChaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y < toPlayerYDist) {
                        Projectile.velocity.Y += idleChaseAccel;
                        if (Projectile.velocity.Y < 0f) {
                            Projectile.velocity.Y += idleChaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y > toPlayerYDist) {
                        Projectile.velocity.Y -= idleChaseAccel;
                        if (Projectile.velocity.Y > 0f) {
                            Projectile.velocity.Y -= idleChaseAccel * 1.5f;
                        }
                    }

                    break;
                }
                default:
                    break;
            }


            if (Projectile.frame == ANIM_CHANGE_REQUESTED) {
                switch (animationState) {
                    case AnimationStates.Idle: {
                        Projectile.frame = 0;
                        break;
                    }
                    case AnimationStates.Running: {
                        Projectile.frame = ANIM_RUN_BEGIN;
                        break;
                    }
                    case AnimationStates.Shooting: {
                        Projectile.frame = ANIM_SHOOT_BEGIN;
                        break;
                    }
                    case AnimationStates.Flying: {
                        Projectile.frame = ANIM_FLY_BEGIN;
                        break;
                    }
                }
            }


            //actual animating
            switch (animationState) {
                case AnimationStates.Idle: {
                    // do nothing! only one idle frame
                    break;
                }
                case AnimationStates.Running: {
                    Projectile.rotation = 0;
                    int frameInterval = 8 - (int)(Math.Abs(Projectile.velocity.X) / 2);
                    frameInterval = Math.Max(frameInterval, 2);
                    if (++Projectile.frameCounter >= frameInterval) {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                        if (Projectile.frame >= ANIM_RUN_BEGIN + ANIM_RUN_FRAMES - 1) {
                            Projectile.frame = ANIM_RUN_BEGIN;
                        }
                    }
                    break;
                }
                case AnimationStates.Shooting: {
                    Projectile.rotation = 0;
                    if (++Projectile.frameCounter >= 3) {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                        if (Projectile.frame >= ANIM_SHOOT_BEGIN + ANIM_SHOOT_FRAMES - 1) {
                            Projectile.frame = ANIM_SHOOT_BEGIN;
                        }
                    }
                    break;
                }
                case AnimationStates.Flying: {
                    int frameInterval = 5 - (int)(Math.Abs(Projectile.velocity.Length()) / 6);
                    frameInterval = Math.Max(frameInterval, 2);
                    if (++Projectile.frameCounter >= frameInterval) {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                        if (Projectile.frame >= ANIM_FLY_BEGIN + ANIM_FLY_FRAMES - 1) {
                            Projectile.frame = ANIM_FLY_BEGIN;
                        }
                    }
                    break;
                }
            }
        }
    }
}
