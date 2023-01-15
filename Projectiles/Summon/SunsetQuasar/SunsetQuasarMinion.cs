using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Summon.SunsetQuasar
{
    internal class SunsetQuasarMinion : ModProjectile {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Sunset Quasar");
            Main.projFrames[Projectile.type] = 27;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ContinuouslyUpdateDamage = true;
            DrawOffsetX = -14;
            DrawOriginOffsetY = -14;
        }

        const float SCALING_PER_SLOT = 0.8f;

        public override bool MinionContactDamage() {
            return true;
        }
        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.SunsetQuasarBuff>())) {
                Projectile.timeLeft = 2;
            }

            StatModifier summon = player.GetTotalDamage(DamageClass.Summon);
            int damage = (int)summon.ApplyTo(Projectile.originalDamage);
            int tokenCount = player.ownedProjectileCounts[ModContent.ProjectileType<SunsetQuasarToken>()];

            float tokenMod = SCALING_PER_SLOT * (tokenCount - 1);
            if (Main.hardMode) tokenMod *= 1.8f; //nothing to see here
            if (tsorcRevampWorld.SuperHardMode) tokenMod *= 2.3f; //not me obviously biased in favor of this weapon or anything
            //i think its cute and want people to use it. sue me.
            float finalDamage = damage * (1.0f + tokenMod);
            Projectile.damage = (int)finalDamage;
            Lighting.AddLight(Projectile.Center, 0.35f, 0.25f, 0.35f);
            SunsetQuasar();
        }
        private float AI_State {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float AI_Timer {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private enum AI_States {
            Combat = 0,
            ChaseOwner = 1
        }

        private enum AnimationStates { 
            Idle,
            Jumping,
            Bubble,
            Running
        }

        AnimationStates animationState = AnimationStates.Idle;
        const int ANIM_BUBBLE = 1;

        const int ANIM_IDLE_BEGIN = 2;
        const int ANIM_IDLE_FRAMES = 9;

        const int ANIM_JUMP_BEGIN = 11;
        const int ANIM_JUMP_FRAMES = 4;

        const int ANIM_RUN_BEGIN = 15;
        const int ANIM_RUN_FRAMES = 12;

        //dont look at this, its still got decompiler names
        private void SunsetQuasar() {
            Player player = Main.player[Projectile.owner];
            int targettingRange = 800;
            float num12 = 500f;
            float num21 = 300f;

            Vector2 targetCenter = player.Center;
            
            targetCenter.X -= (30 + player.width / 2) * player.direction;
            targetCenter.X -= Projectile.minionPos * 30 * player.direction;
            
            Projectile.shouldFallThrough = player.position.Y + (float)player.height > Projectile.position.Y + (float)Projectile.height;

            int num49 = 0;
            int num50 = 15;
            int attackTarget = -1;
            bool inCombat = AI_State == (float)AI_States.Combat;
            if (inCombat) {
                Projectile.Minion_FindTargetInRange(targettingRange, ref attackTarget, skipIfCannotHitWithOwnBody: true);
            }
            if (AI_State == (float)AI_States.ChaseOwner) {
                Projectile.tileCollide = false;
                float chaseAccel = 0.4f;
                float chaseVel = 30f;
                int resetDist = 200;
                if (chaseVel < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y)) {
                    chaseVel = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                }
                Vector2 toPlayer = player.Center - Projectile.Center;
                float toPlayerLength = toPlayer.Length();
                if (toPlayerLength > 2000f) {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                if (toPlayerLength < (float)resetDist && player.velocity.Y == 0f && Projectile.position.Y + (float)Projectile.height <= player.position.Y + (float)player.height && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                    AI_State = (float)AI_States.Combat;
                    Projectile.netUpdate = true;
                    if (Projectile.velocity.Y < -6f) {
                        Projectile.velocity.Y = -6f;
                    }
                }
                if (!(toPlayerLength < 60f)) {
                    toPlayer.Normalize();
                    toPlayer *= chaseVel;
                    if (Projectile.velocity.X < toPlayer.X) {
                        Projectile.velocity.X += chaseAccel;
                        if (Projectile.velocity.X < 0f) {
                            Projectile.velocity.X += chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.X > toPlayer.X) {
                        Projectile.velocity.X -= chaseAccel;
                        if (Projectile.velocity.X > 0f) {
                            Projectile.velocity.X -= chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y < toPlayer.Y) {
                        Projectile.velocity.Y += chaseAccel;
                        if (Projectile.velocity.Y < 0f) {
                            Projectile.velocity.Y += chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y > toPlayer.Y) {
                        Projectile.velocity.Y -= chaseAccel;
                        if (Projectile.velocity.Y > 0f) {
                            Projectile.velocity.Y -= chaseAccel * 1.5f;
                        }
                    }
                    if (animationState != AnimationStates.Bubble) {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Bubble;
                    }
                }
                if (Projectile.velocity.X != 0f) {
                    Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                }

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3) {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame < 2 || Projectile.frame >= Main.projFrames[Projectile.type]) {
                    Projectile.frame = 2;
                }
                //Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.rotation + 0.25f * (float)Projectile.spriteDirection, 0.25f);

            }
            if (AI_State == 2f && AI_Timer < 0f) {
                AI_Timer += 1f;
                if (num50 >= 0) {
                    AI_Timer = 0f;
                    AI_State = (float)AI_States.Combat;
                    Projectile.netUpdate = true;
                    return;
                }
            }
            else if (AI_State == 2f) {
                Projectile.spriteDirection = Projectile.direction;
                Projectile.rotation = 0f;
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 10f) {
                    Projectile.velocity.Y = 10f;
                }
                AI_Timer -= 1f;
                if (AI_Timer <= 0f) {
                    if (num49 <= 0) {
                        AI_Timer = 0f;
                        AI_State = (float)AI_States.Combat;
                        Projectile.netUpdate = true;
                        return;
                    }
                    AI_Timer = -num49;
                }
            }
            if (attackTarget >= 0) {
                float maxDistance2 = targettingRange;
                float num16 = 20f;
                NPC target = Main.npc[attackTarget];
                Vector2 center = target.Center;
                targetCenter = center;
                if (Projectile.IsInRangeOfMeOrMyOwner(target, maxDistance2, out float myDistance, out float playerDistance, out bool closerIsMe)) {
                    Projectile.shouldFallThrough = target.Center.Y > Projectile.Bottom.Y;
                    bool standing = Projectile.velocity.Y == 0f;
                    if (Projectile.wet && Projectile.velocity.Y > 0f && !Projectile.shouldFallThrough) {
                        standing = true;
                    }
                    if (center.Y < Projectile.Center.Y - 30f && standing) {
                        float num52 = (center.Y - Projectile.Center.Y) * -1f;
                        float num17 = 0.4f;
                        float num18 = (float)Math.Sqrt(num52 * 2f * num17);
                        if (num18 > 26f) {
                            num18 = 26f;
                        }
                        Projectile.velocity.Y = 0f - num18;
                    }
                    if (Vector2.Distance(Projectile.Center, targetCenter) < num16) {
                        if (Projectile.velocity.Length() > 10f) {
                            Projectile.velocity /= Projectile.velocity.Length() / 10f;
                        }
                        AI_State = 2f;
                        AI_Timer = num50;
                        Projectile.netUpdate = true;
                        Projectile.direction = ((center.X - Projectile.Center.X > 0f) ? 1 : (-1));
                    }
                }
            }
            if (AI_State == (float)AI_States.Combat && attackTarget < 0) {
                if (Main.player[Projectile.owner].rocketDelay2 > 0) {
                    AI_State = (float)AI_States.ChaseOwner;
                    Projectile.netUpdate = true;
                }
                Vector2 vector8 = player.Center - Projectile.Center;
                if (vector8.Length() > 2000f) {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                else if (vector8.Length() > num12 || Math.Abs(vector8.Y) > num21) {
                    AI_State = (float)AI_States.ChaseOwner;
                    Projectile.netUpdate = true;
                    if (Projectile.velocity.Y > 0f && vector8.Y < 0f) {
                        Projectile.velocity.Y = 0f;
                    }
                    if (Projectile.velocity.Y < 0f && vector8.Y > 0f) {
                        Projectile.velocity.Y = 0f;
                    }
                }
            }
            if (AI_State == (float)AI_States.Combat) {
                if (attackTarget < 0) {
                    if (Projectile.Center.Distance(player.Center) > 60f && Projectile.Center.Distance(targetCenter) > 60f && Math.Sign(targetCenter.X - player.Center.X) != Math.Sign(Projectile.Center.X - player.Center.X)) {
                        targetCenter = player.Center;
                    }
                    Rectangle r = Utils.CenteredRectangle(targetCenter, Projectile.Size);
                    for (int i = 0; i < 20; i++) {
                        if (Collision.SolidCollision(r.TopLeft(), r.Width, r.Height)) {
                            break;
                        }
                        r.Y += 16;
                        targetCenter.Y += 16f;
                    }
                    Vector2 value = Collision.TileCollision(player.Center - Projectile.Size / 2f, targetCenter - player.Center, Projectile.width, Projectile.height);
                    targetCenter = player.Center - Projectile.Size / 2f + value;
                    if (Projectile.Center.Distance(targetCenter) < 32f) {
                        float num23 = player.Center.Distance(targetCenter);
                        if (player.Center.Distance(Projectile.Center) < num23) {
                            targetCenter = Projectile.Center;
                        }
                    }
                    Vector2 vector9 = player.Center - targetCenter;
                    if (vector9.Length() > num12 || Math.Abs(vector9.Y) > num21) {
                        Rectangle r2 = Utils.CenteredRectangle(player.Center, Projectile.Size);
                        Vector2 value2 = targetCenter - player.Center;
                        Vector2 value3 = r2.TopLeft();
                        for (float num24 = 0f; num24 < 1f; num24 += 0.05f) {
                            Vector2 vector10 = r2.TopLeft() + value2 * num24;
                            if (Collision.SolidCollision(r2.TopLeft() + value2 * num24, r.Width, r.Height)) {
                                break;
                            }
                            value3 = vector10;
                        }
                        targetCenter = value3 + Projectile.Size / 2f;
                    }
                }
                Projectile.tileCollide = true;
                float num25 = 0.5f;
                float num26 = 4f;
                float num27 = 4f;
                float num28 = 0.1f;
                if (attackTarget != -1) {
                    num25 = 0.65f;
                    num26 = 5.5f;
                    num27 = 5.5f;
                }
                if (num27 < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y)) {
                    num27 = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                    num25 = 0.7f;
                }
                int num29 = 0;
                bool flag4 = false;
                float num30 = targetCenter.X - Projectile.Center.X;
                Vector2 toPlayer = targetCenter - Projectile.Center;
                if (Math.Abs(num30) > 5f) {
                    if (num30 < 0f) {
                        num29 = -1;
                        if (Projectile.velocity.X > 0f - num26) {
                            Projectile.velocity.X -= num25;
                        }
                        else {
                            Projectile.velocity.X -= num28;
                        }
                    }
                    else {
                        num29 = 1;
                        if (Projectile.velocity.X < num26) {
                            Projectile.velocity.X += num25;
                        }
                        else {
                            Projectile.velocity.X += num28;
                        }
                    }
                    bool contactingTarget = attackTarget > -1 && Main.npc[attackTarget].Hitbox.Intersects(Projectile.Hitbox);
                    
                    if (contactingTarget) {
                        flag4 = true;
                    }
                }
                else {
                    Projectile.velocity.X *= 0.9f;
                    if (Math.Abs(Projectile.velocity.X) < num25 * 2f) {
                        Projectile.velocity.X = 0f;
                    }
                }
                bool flag6 = Math.Abs(toPlayer.X) >= 64f || (toPlayer.Y <= -48f && Math.Abs(toPlayer.X) >= 8f);
                if (num29 != 0 && flag6) {
                    int num31 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                    int num33 = (int)Projectile.position.Y / 16;
                    num31 += num29;
                    num31 += (int)Projectile.velocity.X;
                    for (int j = num33; j < num33 + Projectile.height / 16 + 1; j++) {
                        if (WorldGen.SolidTile(num31, j)) {
                            flag4 = true;
                        }
                    }
                }

                if (player.velocity.Y == 0f) flag4 = false;
                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
                float num34 = Utils.GetLerpValue(0f, 100f, toPlayer.Y, clamped: true) * Utils.GetLerpValue(-2f, -6f, Projectile.velocity.Y, clamped: true);
                if (Projectile.velocity.Y == 0f && flag4) {
                    for (int k = 0; k < 3; k++) {
                        int num35 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                        if (k == 0) {
                            num35 = (int)Projectile.position.X / 16;
                        }
                        if (k == 2) {
                            num35 = (int)(Projectile.position.X + (float)Projectile.width) / 16;
                        }
                        int projGroundTileY = (int)(Projectile.position.Y + (float)Projectile.height) / 16;
                        if (!WorldGen.SolidTile(num35, projGroundTileY) && !Main.tile[num35, projGroundTileY].IsHalfBlock && Main.tile[num35, projGroundTileY].Slope <= 0 && (!TileID.Sets.Platforms[Main.tile[num35, projGroundTileY].TileType] || !Main.tile[num35, projGroundTileY].HasTile || Main.tile[num35, projGroundTileY].HasUnactuatedTile)) {
                            continue;
                        }
                        try {
                            num35 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                            projGroundTileY = (int)(Projectile.position.Y + (float)(Projectile.height / 2)) / 16;
                            num35 += num29;
                            num35 += (int)Projectile.velocity.X;
                            if (!WorldGen.SolidTile(num35, projGroundTileY - 1) && !WorldGen.SolidTile(num35, projGroundTileY - 2)) {
                                Projectile.velocity.Y = -5.1f;
                            }
                            else if (!WorldGen.SolidTile(num35, projGroundTileY - 2)) {
                                Projectile.velocity.Y = -7.1f;
                            }
                            else if (WorldGen.SolidTile(num35, projGroundTileY - 5)) {
                                Projectile.velocity.Y = -11.1f;
                            }
                            else if (WorldGen.SolidTile(num35, projGroundTileY - 4)) {
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
                    if (targetCenter.Y - Projectile.Center.Y < -48f) {
                        float num37 = targetCenter.Y - Projectile.Center.Y;
                        num37 *= -1f;
                        if (num37 < 60f) {
                            Projectile.velocity.Y = -6f;
                        }
                        else if (num37 < 80f) {
                            Projectile.velocity.Y = -7f;
                        }
                        else if (num37 < 100f) {
                            Projectile.velocity.Y = -8f;
                        }
                        else if (num37 < 120f) {
                            Projectile.velocity.Y = -9f;
                        }
                        else if (num37 < 140f) {
                            Projectile.velocity.Y = -10f;
                        }
                        else if (num37 < 160f) {
                            Projectile.velocity.Y = -11f;
                        }
                        else if (num37 < 190f) {
                            Projectile.velocity.Y = -12f;
                        }
                        else if (num37 < 210f) {
                            Projectile.velocity.Y = -13f;
                        }
                        else if (num37 < 270f) {
                            Projectile.velocity.Y = -14f;
                        }
                        else if (num37 < 310f) {
                            Projectile.velocity.Y = -15f;
                        }
                        else {
                            Projectile.velocity.Y = -16f;
                        }
                    }
                    if (Projectile.wet && num34 == 0f) {
                        Projectile.velocity.Y *= 2f;
                    }
                }
                if (Projectile.velocity.X > num27) {
                    Projectile.velocity.X = num27;
                }
                if (Projectile.velocity.X < 0f - num27) {
                    Projectile.velocity.X = 0f - num27;
                }
                if (Projectile.velocity.X < 0f) {
                    Projectile.direction = -1;
                }
                if (Projectile.velocity.X > 0f) {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X == 0f) {
                    Projectile.direction = ((player.Center.X > Projectile.Center.X) ? 1 : (-1));
                }
                if (Projectile.velocity.X > num25 && num29 == 1) {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X < 0f - num25 && num29 == -1) {
                    Projectile.direction = -1;
                }
                Projectile.spriteDirection = Projectile.direction;
                
                if (Projectile.velocity == Vector2.Zero) {
                    if (animationState != AnimationStates.Idle) {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Idle; 
                    }
                }
                else if (Projectile.velocity.Y != 0f) {
                    if (Projectile.spriteDirection == -1) {
                        Projectile.rotation -= (float)Math.PI * 2f;
                    }
                    if (animationState != AnimationStates.Jumping) {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Jumping;
                    }
                }

                else {
                    if (animationState != AnimationStates.Running) {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Running;
                    }
                }
                

                Projectile.velocity.Y += 0.4f + num34 * 1f;
                if (Projectile.velocity.Y > 10f) {
                    Projectile.velocity.Y = 10f;
                }
            }

            //animation state change requested
            if (Projectile.frame == 0) {
                switch (animationState) {
                    case AnimationStates.Idle: {
                        Projectile.frame = ANIM_IDLE_BEGIN;
                        break;
                    }
                    case AnimationStates.Jumping: {
                        Projectile.frame = ANIM_JUMP_BEGIN;
                        break;
                    }
                    case AnimationStates.Running: {
                        Projectile.frame = ANIM_RUN_BEGIN;
                        break;
                    }
                    default:break;
                }
            }


            //actual animating
            switch (animationState) {
                case AnimationStates.Idle: {
                    Projectile.rotation = 0;
                    if (++Projectile.frameCounter >= 12) {
                        Projectile.frameCounter = 0;
                        if (++Projectile.frame >= ANIM_IDLE_FRAMES) {
                            Projectile.frame = ANIM_IDLE_BEGIN;
                        }
                    }
                    break;
                }
                case AnimationStates.Jumping: {
                    Projectile.rotation = 0;
                    if (++Projectile.frameCounter >= 8) {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                        if (Projectile.frame >= ANIM_JUMP_BEGIN + ANIM_JUMP_FRAMES - 1) {
                            if (Projectile.velocity.Y > 0.5f) {
                                Projectile.frame = ANIM_JUMP_BEGIN + ANIM_JUMP_FRAMES - 1;
                            }
                            else {
                                Projectile.frame = ANIM_JUMP_BEGIN;
                            }
                        }
                    }
                    break;
                }
                case AnimationStates.Running: {
                    Projectile.rotation = 0;
                    if (++Projectile.frameCounter >= 4) {
                        Projectile.frameCounter = 0;
                        if (++Projectile.frame > ANIM_RUN_BEGIN + ANIM_RUN_FRAMES - 1) {
                            Projectile.frame = ANIM_RUN_BEGIN;
                        }
                    }
                    break;
                }
                case AnimationStates.Bubble: {
                    Projectile.frame = ANIM_BUBBLE;
                    Projectile.frameCounter = 0;
                    break;
                }
                default:break;
            }
        }

    }
}
