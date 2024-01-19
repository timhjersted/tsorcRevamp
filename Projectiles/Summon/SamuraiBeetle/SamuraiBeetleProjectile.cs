using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Throwing;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    public class SamuraiBeetleProjectile : ModProjectile
    {
        public const string SoundPath = "tsorcRevamp/Sounds/CrossCode/";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 36;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            Main.projPet[Projectile.type] = true;

            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 30;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            DrawOffsetX = -31;
            DrawOriginOffsetY = -30;
        }
        NPC ActualTarget;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!CheckActive(player))
            {
                return;
            }
            SunsetQuasar(out Vector2 SunsetQuasarVel);
            GeneralBehavior(player, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
            SearchForTargets(player, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            Movement(foundTarget, distanceFromTarget, targetCenter, distanceToIdlePosition, vectorToIdlePosition, out float speed, out float inertia, out Vector2 direction, out float attackRange, out bool IsFlying, player);
            if (foundTarget)
            {
                Attacking(targetCenter, SunsetQuasarVel);
            }
            Visuals(foundTarget, attackRange, targetCenter, IsFlying);
        }
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<SamuraiBeetleBuff>());

                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<SamuraiBeetleBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public bool CanHit = false;
        public override bool MinionContactDamage()
        {
            return CanHit;
        }
        private float AI_State
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float AI_Timer
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private enum AI_States
        {
            Combat = 0,
            ChaseOwner = 1
        }

        private enum AnimationStates
        {
            Idle,
            Jumping,
            Flying,
            Running
        }

        AnimationStates animationState = AnimationStates.Idle;

        const int ANIM_IDLE_BEGIN = 1;
        const int ANIM_IDLE_FRAMES = 1;

        const int ANIM_JUMP_BEGIN = 2;
        const int ANIM_JUMP_FRAMES = 6;

        const int ANIM_RUN_BEGIN = 8;
        const int ANIM_RUN_FRAMES = 8;

        const int ANIM_THRUST_BEGIN = 16;
        const int ANIM_THRUST_FRAMES = 8;

        const int ANIM_SLASH_BEGIN = 24;
        const int ANIM_SLASH_FRAMES = 8;

        const int ANIM_FLYING_BEGIN = 32;
        const int ANIM_FLYING_FRAMES = 5;
        private void SunsetQuasar(out Vector2 SunsetQuasarVel)
        {
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
            if (inCombat)
            {
                Projectile.Minion_FindTargetInRange(targettingRange, ref attackTarget, skipIfCannotHitWithOwnBody: true);
            }
            if (AI_State == (float)AI_States.ChaseOwner)
            {
                Projectile.tileCollide = false;
                float chaseAccel = 0.4f;
                float chaseVel = 30f;
                int resetDist = 200;
                if (chaseVel < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y))
                {
                    chaseVel = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                }
                Vector2 toPlayer = player.Center - Projectile.Center;
                float toPlayerLength = toPlayer.Length();
                if (toPlayerLength > 2000f)
                {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                if (toPlayerLength < (float)resetDist && player.velocity.Y == 0f && Projectile.position.Y + (float)Projectile.height <= player.position.Y + (float)player.height && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    AI_State = (float)AI_States.Combat;
                    Projectile.netUpdate = true;
                    if (Projectile.velocity.Y < -6f)
                    {
                        Projectile.velocity.Y = -6f;
                    }
                }
                if (!(toPlayerLength < 60f))
                {
                    toPlayer.Normalize();
                    toPlayer *= chaseVel;
                    if (Projectile.velocity.X < toPlayer.X)
                    {
                        Projectile.velocity.X += chaseAccel;
                        if (Projectile.velocity.X < 0f)
                        {
                            Projectile.velocity.X += chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.X > toPlayer.X)
                    {
                        Projectile.velocity.X -= chaseAccel;
                        if (Projectile.velocity.X > 0f)
                        {
                            Projectile.velocity.X -= chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y < toPlayer.Y)
                    {
                        Projectile.velocity.Y += chaseAccel;
                        if (Projectile.velocity.Y < 0f)
                        {
                            Projectile.velocity.Y += chaseAccel * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y > toPlayer.Y)
                    {
                        Projectile.velocity.Y -= chaseAccel;
                        if (Projectile.velocity.Y > 0f)
                        {
                            Projectile.velocity.Y -= chaseAccel * 1.5f;
                        }
                    }
                    if (animationState != AnimationStates.Flying)
                    {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Flying;
                    }
                }
                if (Projectile.velocity.X != 0f)
                {
                    Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                }

                Projectile.frameCounter++;
                if (Projectile.frameCounter > 3)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame < 2 || Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 2;
                }
                //Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.rotation + 0.25f * (float)Projectile.spriteDirection, 0.25f);

            }
            if (AI_State == 2f && AI_Timer < 0f)
            {
                AI_Timer += 1f;
                if (num50 >= 0)
                {
                    AI_Timer = 0f;
                    AI_State = (float)AI_States.Combat;
                    Projectile.netUpdate = true;
                    SunsetQuasarVel = Projectile.velocity;
                    return;
                }
            }
            else if (AI_State == 2f)
            {
                Projectile.spriteDirection = Projectile.direction;
                Projectile.rotation = 0f;
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
                AI_Timer -= 1f;
                if (AI_Timer <= 0f)
                {
                    if (num49 <= 0)
                    {
                        AI_Timer = 0f;
                        AI_State = (float)AI_States.Combat;
                        Projectile.netUpdate = true;
                        SunsetQuasarVel = Projectile.velocity;
                        return;
                    }
                    AI_Timer = -num49;
                }
            }
            if (attackTarget >= 0)
            {
                float maxDistance2 = targettingRange;
                float num16 = 20f;
                NPC target = Main.npc[attackTarget];
                Vector2 center = target.Center;
                targetCenter = center;
                if (Projectile.IsInRangeOfMeOrMyOwner(target, maxDistance2, out float myDistance, out float playerDistance, out bool closerIsMe))
                {
                    Projectile.shouldFallThrough = target.Center.Y > Projectile.Bottom.Y;
                    bool standing = Projectile.velocity.Y == 0f;
                    if (Projectile.wet && Projectile.velocity.Y > 0f && !Projectile.shouldFallThrough)
                    {
                        standing = true;
                    }
                    if (center.Y < Projectile.Center.Y - 30f && standing)
                    {
                        float num52 = (center.Y - Projectile.Center.Y) * -1f;
                        float num17 = 0.4f;
                        float num18 = (float)Math.Sqrt(num52 * 2f * num17);
                        if (num18 > 26f)
                        {
                            num18 = 26f;
                        }
                        Projectile.velocity.Y = 0f - num18;
                    }
                    if (Vector2.Distance(Projectile.Center, targetCenter) < num16)
                    {
                        if (Projectile.velocity.Length() > 10f)
                        {
                            Projectile.velocity /= Projectile.velocity.Length() / 10f;
                        }
                        AI_State = 2f;
                        AI_Timer = num50;
                        Projectile.netUpdate = true;
                        Projectile.direction = ((center.X - Projectile.Center.X > 0f) ? 1 : (-1));
                    }
                }
            }
            if (AI_State == (float)AI_States.Combat && attackTarget < 0)
            {
                if (Main.player[Projectile.owner].rocketDelay2 > 0)
                {
                    AI_State = (float)AI_States.ChaseOwner;
                    Projectile.netUpdate = true;
                }
                Vector2 vector8 = player.Center - Projectile.Center;
                if (vector8.Length() > 2000f)
                {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                else if (vector8.Length() > num12 || Math.Abs(vector8.Y) > num21)
                {
                    AI_State = (float)AI_States.ChaseOwner;
                    Projectile.netUpdate = true;
                    if (Projectile.velocity.Y > 0f && vector8.Y < 0f)
                    {
                        Projectile.velocity.Y = 0f;
                    }
                    if (Projectile.velocity.Y < 0f && vector8.Y > 0f)
                    {
                        Projectile.velocity.Y = 0f;
                    }
                }
            }
            if (AI_State == (float)AI_States.Combat)
            {
                if (attackTarget < 0)
                {
                    if (Projectile.Center.Distance(player.Center) > 60f && Projectile.Center.Distance(targetCenter) > 60f && Math.Sign(targetCenter.X - player.Center.X) != Math.Sign(Projectile.Center.X - player.Center.X))
                    {
                        targetCenter = player.Center;
                    }
                    Rectangle r = Utils.CenteredRectangle(targetCenter, Projectile.Size);
                    for (int i = 0; i < 20; i++)
                    {
                        if (Collision.SolidCollision(r.TopLeft(), r.Width, r.Height))
                        {
                            break;
                        }
                        r.Y += 16;
                        targetCenter.Y += 16f;
                    }
                    Vector2 value = Collision.TileCollision(player.Center - Projectile.Size / 2f, targetCenter - player.Center, Projectile.width, Projectile.height);
                    targetCenter = player.Center - Projectile.Size / 2f + value;
                    if (Projectile.Center.Distance(targetCenter) < 32f)
                    {
                        float num23 = player.Center.Distance(targetCenter);
                        if (player.Center.Distance(Projectile.Center) < num23)
                        {
                            targetCenter = Projectile.Center;
                        }
                    }
                    Vector2 vector9 = player.Center - targetCenter;
                    if (vector9.Length() > num12 || Math.Abs(vector9.Y) > num21)
                    {
                        Rectangle r2 = Utils.CenteredRectangle(player.Center, Projectile.Size);
                        Vector2 value2 = targetCenter - player.Center;
                        Vector2 value3 = r2.TopLeft();
                        for (float num24 = 0f; num24 < 1f; num24 += 0.05f)
                        {
                            Vector2 vector10 = r2.TopLeft() + value2 * num24;
                            if (Collision.SolidCollision(r2.TopLeft() + value2 * num24, r.Width, r.Height))
                            {
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
                if (attackTarget != -1)
                {
                    num25 = 0.65f;
                    num26 = 5.5f;
                    num27 = 5.5f;
                }
                if (num27 < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y))
                {
                    num27 = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                    num25 = 0.7f;
                }
                int num29 = 0;
                bool flag4 = false;
                float num30 = targetCenter.X - Projectile.Center.X;
                Vector2 toPlayer = targetCenter - Projectile.Center;
                if (Math.Abs(num30) > 5f)
                {
                    if (num30 < 0f)
                    {
                        num29 = -1;
                        if (Projectile.velocity.X > 0f - num26)
                        {
                            Projectile.velocity.X -= num25;
                        }
                        else
                        {
                            Projectile.velocity.X -= num28;
                        }
                    }
                    else
                    {
                        num29 = 1;
                        if (Projectile.velocity.X < num26)
                        {
                            Projectile.velocity.X += num25;
                        }
                        else
                        {
                            Projectile.velocity.X += num28;
                        }
                    }
                    bool contactingTarget = attackTarget > -1 && Main.npc[attackTarget].Hitbox.Intersects(Projectile.Hitbox);

                    if (contactingTarget)
                    {
                        flag4 = true;
                    }
                }
                else
                {
                    Projectile.velocity.X *= 0.9f;
                    if (Math.Abs(Projectile.velocity.X) < num25 * 2f)
                    {
                        Projectile.velocity.X = 0f;
                    }
                }
                bool flag6 = Math.Abs(toPlayer.X) >= 64f || (toPlayer.Y <= -48f && Math.Abs(toPlayer.X) >= 8f);
                if (num29 != 0 && flag6)
                {
                    int num31 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                    int num33 = (int)Projectile.position.Y / 16;
                    num31 += num29;
                    num31 += (int)Projectile.velocity.X;
                    for (int j = num33; j < num33 + Projectile.height / 16 + 1; j++)
                    {
                        if (WorldGen.SolidTile(num31, j))
                        {
                            flag4 = true;
                        }
                    }
                }

                if (player.velocity.Y == 0f) flag4 = false;
                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
                float num34 = Utils.GetLerpValue(0f, 100f, toPlayer.Y, clamped: true) * Utils.GetLerpValue(-2f, -6f, Projectile.velocity.Y, clamped: true);
                if (Projectile.velocity.Y == 0f && flag4)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int num35 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                        if (k == 0)
                        {
                            num35 = (int)Projectile.position.X / 16;
                        }
                        if (k == 2)
                        {
                            num35 = (int)(Projectile.position.X + (float)Projectile.width) / 16;
                        }
                        int projGroundTileY = (int)(Projectile.position.Y + (float)Projectile.height) / 16;
                        if (!WorldGen.SolidTile(num35, projGroundTileY) && !Main.tile[num35, projGroundTileY].IsHalfBlock && Main.tile[num35, projGroundTileY].Slope <= 0 && (!TileID.Sets.Platforms[Main.tile[num35, projGroundTileY].TileType] || !Main.tile[num35, projGroundTileY].HasTile || Main.tile[num35, projGroundTileY].HasUnactuatedTile))
                        {
                            continue;
                        }
                        try
                        {
                            num35 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                            projGroundTileY = (int)(Projectile.position.Y + (float)(Projectile.height / 2)) / 16;
                            num35 += num29;
                            num35 += (int)Projectile.velocity.X;
                            if (!WorldGen.SolidTile(num35, projGroundTileY - 1) && !WorldGen.SolidTile(num35, projGroundTileY - 2))
                            {
                                Projectile.velocity.Y = -5.1f;
                            }
                            else if (!WorldGen.SolidTile(num35, projGroundTileY - 2))
                            {
                                Projectile.velocity.Y = -7.1f;
                            }
                            else if (WorldGen.SolidTile(num35, projGroundTileY - 5))
                            {
                                Projectile.velocity.Y = -11.1f;
                            }
                            else if (WorldGen.SolidTile(num35, projGroundTileY - 4))
                            {
                                Projectile.velocity.Y = -10.1f;
                            }
                            else
                            {
                                Projectile.velocity.Y = -9.1f;
                            }
                        }
                        catch
                        {
                            Projectile.velocity.Y = -9.1f;
                        }
                    }
                    if (targetCenter.Y - Projectile.Center.Y < -48f)
                    {
                        float num37 = targetCenter.Y - Projectile.Center.Y;
                        num37 *= -1f;
                        if (num37 < 60f)
                        {
                            Projectile.velocity.Y = -6f;
                        }
                        else if (num37 < 80f)
                        {
                            Projectile.velocity.Y = -7f;
                        }
                        else if (num37 < 100f)
                        {
                            Projectile.velocity.Y = -8f;
                        }
                        else if (num37 < 120f)
                        {
                            Projectile.velocity.Y = -9f;
                        }
                        else if (num37 < 140f)
                        {
                            Projectile.velocity.Y = -10f;
                        }
                        else if (num37 < 160f)
                        {
                            Projectile.velocity.Y = -11f;
                        }
                        else if (num37 < 190f)
                        {
                            Projectile.velocity.Y = -12f;
                        }
                        else if (num37 < 210f)
                        {
                            Projectile.velocity.Y = -13f;
                        }
                        else if (num37 < 270f)
                        {
                            Projectile.velocity.Y = -14f;
                        }
                        else if (num37 < 310f)
                        {
                            Projectile.velocity.Y = -15f;
                        }
                        else
                        {
                            Projectile.velocity.Y = -16f;
                        }
                    }
                    if (Projectile.wet && num34 == 0f)
                    {
                        Projectile.velocity.Y *= 2f;
                    }
                }
                if (Projectile.velocity.X > num27)
                {
                    Projectile.velocity.X = num27;
                }
                if (Projectile.velocity.X < 0f - num27)
                {
                    Projectile.velocity.X = 0f - num27;
                }
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.direction = -1;
                }
                if (Projectile.velocity.X > 0f)
                {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X == 0f)
                {
                    Projectile.direction = ((player.Center.X > Projectile.Center.X) ? 1 : (-1));
                }
                if (Projectile.velocity.X > num25 && num29 == 1)
                {
                    Projectile.direction = 1;
                }
                if (Projectile.velocity.X < 0f - num25 && num29 == -1)
                {
                    Projectile.direction = -1;
                }
                Projectile.spriteDirection = Projectile.direction;

                if (Projectile.velocity == Vector2.Zero)
                {
                    if (animationState != AnimationStates.Idle)
                    {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Idle;
                    }
                }
                else if (Projectile.velocity.Y != 0f)
                {
                    if (Projectile.spriteDirection == -1)
                    {
                        Projectile.rotation -= (float)Math.PI * 2f;
                    }
                    if (animationState != AnimationStates.Jumping)
                    {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Jumping;
                    }
                }

                else
                {
                    if (animationState != AnimationStates.Running)
                    {
                        Projectile.frame = 0;
                        animationState = AnimationStates.Running;
                    }
                }


                Projectile.velocity.Y += 0.4f + num34 * 1f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
            }

            //animation state change requested
            if (Projectile.frame == 0)
            {
                switch (animationState)
                {
                    case AnimationStates.Idle:
                        {
                            Projectile.frame = ANIM_IDLE_BEGIN;
                            break;
                        }
                    case AnimationStates.Jumping:
                        {
                            Projectile.frame = ANIM_JUMP_BEGIN;
                            break;
                        }
                    case AnimationStates.Running:
                        {
                            Projectile.frame = ANIM_RUN_BEGIN;
                            break;
                        }
                    case AnimationStates.Flying:
                        {
                            Projectile.frame = ANIM_FLYING_BEGIN;
                            break;
                        }
                    default: break;
                }
            }


            //actual animating
            switch (animationState)
            {
                case AnimationStates.Idle:
                    {
                        Projectile.rotation = 0;
                        if (++Projectile.frameCounter >= 12)
                        {
                            Projectile.frameCounter = 0;
                            if (++Projectile.frame >= ANIM_IDLE_FRAMES)
                            {
                                Projectile.frame = ANIM_IDLE_BEGIN;
                            }
                        }
                        break;
                    }
                case AnimationStates.Jumping:
                    {
                        Projectile.rotation = 0;
                        if (++Projectile.frameCounter >= 8)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame++;
                            if (Projectile.frame >= ANIM_JUMP_BEGIN + ANIM_JUMP_FRAMES - 1)
                            {
                                if (Projectile.velocity.Y > 0.5f)
                                {
                                    Projectile.frame = ANIM_JUMP_BEGIN + ANIM_JUMP_FRAMES - 1;
                                }
                                else
                                {
                                    Projectile.frame = ANIM_JUMP_BEGIN;
                                }
                            }
                        }
                        break;
                    }
                case AnimationStates.Running:
                    {
                        Projectile.rotation = 0;
                        if (++Projectile.frameCounter >= 4)
                        {
                            Projectile.frameCounter = 0;
                            if (++Projectile.frame > ANIM_RUN_BEGIN + ANIM_RUN_FRAMES - 1)
                            {
                                Projectile.frame = ANIM_RUN_BEGIN;
                            }
                        }
                        break;
                    }
                case AnimationStates.Flying:
                    {
                        Projectile.rotation = 0;
                        if (++Projectile.frameCounter >= 4)
                        {
                            Projectile.frameCounter = 0;
                            if (++Projectile.frame > ANIM_FLYING_BEGIN + ANIM_FLYING_FRAMES - 3)
                            {
                                Projectile.frame = ANIM_FLYING_BEGIN;
                            }
                        }
                        break;
                    }
                default: break;
            }
            SunsetQuasarVel = Projectile.velocity;
        }
        private void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
        {
            Vector2 idlePosition = owner.Center;
            idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

            // If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
            // The index is projectile.minionPos
            float minionPositionOffsetX = (10 + Projectile.minionPos * 40) * -owner.direction;
            idlePosition.X += minionPositionOffsetX; // Go behind the player

            // All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

            // Teleport to player if distance is too big
            vectorToIdlePosition = idlePosition - Projectile.Center;
            distanceToIdlePosition = vectorToIdlePosition.Length();

            if (Main.myPlayer == owner.whoAmI && distanceToIdlePosition > 2000f)
            {
                // Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
                // and then set netUpdate to true
                Projectile.position = idlePosition;
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }

            // If your minion is flying, you want to do this independently of any conditions
            float overlapVelocity = 0.04f;

            // Fix overlap with other minions
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.active && other.owner == Projectile.owner && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
                {
                    if (Projectile.position.X < other.position.X)
                    {
                        Projectile.velocity.X -= overlapVelocity;
                    }
                    else
                    {
                        Projectile.velocity.X += overlapVelocity;
                    }

                    if (Projectile.position.Y < other.position.Y)
                    {
                        Projectile.velocity.Y -= overlapVelocity;
                    }
                    else
                    {
                        Projectile.velocity.Y += overlapVelocity;
                    }
                }
            }
        }

        private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter)
        {
            // Starting search distance
            distanceFromTarget = 800f;
            targetCenter = Projectile.position;
            foundTarget = false;

            // This code is required if your minion weapon has the targeting feature
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[owner.MinionAttackTargetNPC];
                float between = Vector2.Distance(npc.Center, Projectile.Center);

                // Reasonable distance away so it doesn't target across multiple screens
                if (between < 2000f)
                {
                    distanceFromTarget = between;
                    targetCenter = npc.Center;
                    foundTarget = true;
                    ActualTarget = npc;
                }
            }

            if (!foundTarget)
            {
                // This code is required either way, used for finding a target
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];

                    if (npc.CanBeChasedBy())
                    {
                        float between = Vector2.Distance(npc.Center, Projectile.Center);
                        bool closest = Vector2.Distance(Projectile.Center, targetCenter) > between;
                        bool inRange = between < distanceFromTarget;
                        bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height);
                        // Additional check for this specific minion behavior, otherwise it will stop attacking once it dashed through an enemy while flying though tiles afterwards
                        // The number depends on various parameters seen in the movement code below. Test different ones out until it works alright
                        bool closeThroughWall = between < 800;

                        if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall))
                        {
                            distanceFromTarget = between;
                            targetCenter = npc.Center;
                            foundTarget = true;
                            ActualTarget = npc;
                        }
                    }
                }
            }

            // friendly needs to be set to true so the minion can deal contact damage
            // friendly needs to be set to false so it doesn't damage things like target dummies while idling
            // Both things depend on if it has a target or not, so it's just one assignment here
            // You don't need this assignment if your minion is shooting things instead of dealing contact damage
            Projectile.friendly = foundTarget;
        }

 
        public int ThrustProgress = 0;
        public int ThrustStacks = 0;
        public bool IsThrusting = false;
        public int SlashProgress = 0;
        public bool IsSlashing = false;
        public bool SlashHit = false;
        public Vector2 DashVelocity;
        public Vector2 CircleVector;
        public bool RandomMovementSet = false;
        private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition, out float speed, out float inertia, out Vector2 direction, out float attackRange, out bool IsFlying, Player player)
        {
            // Default movement parameters (here for attacking)
            speed = 8f;
            inertia = 10f;
            direction = Projectile.Center;
            attackRange = 400f;
            int circleSize = 350;
            IsFlying = false;

            if (foundTarget && distanceToIdlePosition < 1000f)
            {
                if (distanceFromTarget > attackRange)
                {
                    if (!RandomMovementSet)
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            CircleVector = Main.rand.NextVector2CircularEdge(circleSize, circleSize);

                            if (Projectile.Distance(CircleVector + targetCenter) > 400)
                            {
                                break;
                            }
                        }
                        RandomMovementSet = true;
                    }
                    Projectile.velocity = Projectile.DirectionTo(CircleVector + targetCenter) * (CircleVector + targetCenter).Distance(Projectile.Center) / 20;
                    //Dust.NewDust(CircleVector + targetCenter, 10, 10, DustID.Torch);
                } 
                else if (distanceFromTarget <= attackRange)
                {
                    if (!RandomMovementSet)
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            CircleVector = Main.rand.NextVector2CircularEdge(circleSize, circleSize);

                            if (Projectile.Distance(CircleVector + targetCenter) > 400)
                            {
                                break;
                            }
                        }
                        RandomMovementSet = true;
                    }
                    Projectile.velocity = Projectile.DirectionTo(CircleVector + targetCenter) * (CircleVector + targetCenter).Distance(Projectile.Center) / 20;
                    //Dust.NewDust(CircleVector + targetCenter, 10, 10, DustID.Torch);
                    switch (ThrustStacks)
                    {
                        case 0:
                            {
                                IsThrusting = true;
                                break;
                            }
                        case 1:
                            {
                                IsThrusting = true;
                                break;
                            }
                        case 2:
                            {
                                IsThrusting = true;
                                break;
                            }
                        case 3:
                            {
                                IsSlashing = true;
                                break;
                            }
                    }
                }
            }
            else
            {
                // Minion doesn't have a target: return to player and idle
                if (distanceToIdlePosition > 200f)
                {
                    speed = 12f * distanceToIdlePosition / 200;
                    inertia = 60f;
                    IsFlying = true;
                    Projectile.velocity = Projectile.DirectionTo(player.Center) * speed;
                }
            }
        }
        public float FallTimer = 0;
        public bool ShouldRotate = false;
        private void Attacking (Vector2 targetCenter, Vector2 SunsetQuasarVel)
        {
            Vector2 Falling = new Vector2(DashVelocity.X / 15, FallTimer / 8.5f);
            if (IsThrusting)
            {
                Projectile.tileCollide = false;
                ThrustProgress++;
                IsSlashing = false;
                switch (ThrustProgress) 
                {
                    case 0:
                        {
                            RandomMovementSet = false;
                            break;
                        }
                    case 1:
                        {
                            SpawnTrail();
                            //Dust.NewDustDirect(Projectile.Center, 20, 20, DustID.Cloud, DashVelocity.X / 10, DashVelocity.Y / 10, 0, Color.Purple, 2f);
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "samurai-jump") with { Volume = 1f });
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<SamuraiBeetleCloud>(), 0, 0, Projectile.owner);
                            }
                            ShouldRotate = true;
                            break;
                        }
                    case int Preparing  when (Preparing > 1 && Preparing < 45):
                        {
                            break;
                        }
                    case 45:
                        {
                            DashVelocity = Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5;
                            //Dust.NewDustDirect(Projectile.Center, 20, 20, DustID.Cloud, DashVelocity.X / 10, DashVelocity.Y / 10, 0, Color.Purple, 2f);
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "samurai-jump") with { Volume = 1f });
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<SamuraiBeetleCloud>(), 0, 0, Projectile.owner);
                            }
                            Projectile.ResetLocalNPCHitImmunity();
                            CanHit = true;
                            break;
                        }
                    case 46:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.8f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 47:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.6f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 48:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.4f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 49:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.2f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 50:
                        {
                            Projectile.velocity = DashVelocity;
                            //should hit on this tick
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "sword-slide") with { Volume = 1f });
                            break;
                        }
                    case 51:
                        {
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 52:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 53:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 54:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 55:
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            KillTrail();
                            if (FallTimer < 100)
                            {
                                FallTimer++;
                            }
                            RandomMovementSet = false;
                            ShouldRotate = false;
                            //Dust.NewDustDirect(Projectile.Center, 20, 20, DustID.Cloud, DashVelocity.X / 10, DashVelocity.Y / 10, 0, Color.Purple, 2f);
                            CanHit = false;
                            break;
                        }
                    case int Finishing when (Finishing >= 56 && Finishing < 120):
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            if (Finishing > 80)
                            {
                            }
                            if (FallTimer < 100)
                            {
                                FallTimer++;
                            }
                            break;
                        }
                    case 120:
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            FallTimer = 0;
                            IsThrusting = false;
                            ThrustProgress = 0;
                            if (ThrustStacks == 2)
                            {
                                ThrustStacks++;
                            }
                            AttackHit = false;
                            KillTrail();
                            break;
                        }
                    case >120:
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            FallTimer = 0;
                            IsThrusting = false;
                            ThrustProgress = 0;
                            if (ThrustStacks == 2)
                            {
                                ThrustStacks++;
                            }
                            AttackHit = false;
                            break;
                        }
                }
            }
            if (IsSlashing)
            {
                Projectile.tileCollide = false;
                SlashProgress++;
                IsThrusting = false;
                switch (SlashProgress)
                {
                    case 0:
                        {
                            RandomMovementSet = false;
                            break;
                        }
                    case 1:
                        {
                            ShouldRotate = true;
                            SpawnTrail();
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "samurai-jump") with { Volume = 1f });
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<SamuraiBeetleCloud>(), 0, 0, Projectile.owner);
                            }
                            //Dust.NewDustDirect(Projectile.Center, 20, 20, DustID.Cloud, Projectile.velocity.X / 10, Projectile.velocity.Y / 10, 0, Color.Purple, 2f);
                            break;
                        }
                    case int Preparing when (Preparing > 1 && Preparing < 60):
                        {
                            break;
                        }
                    case 60:
                        {
                            DashVelocity = Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5;
                            Projectile.ResetLocalNPCHitImmunity();
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "samurai-jump") with { Volume = 1f });
                            if (Main.myPlayer == Projectile.owner)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<SamuraiBeetleCloud>(), 0, 0, Projectile.owner);
                            }
                            CanHit = true;
                            break;
                        }
                    case 61:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.8f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 62:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.6f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 63:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.4f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 64:
                        {
                            DashVelocity = (Projectile.DirectionTo(targetCenter) * targetCenter.Distance(Projectile.Center) / 5) / 0.2f;
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 65:
                        {
                            Projectile.velocity = DashVelocity;
                            SoundEngine.PlaySound(new SoundStyle(SoundPath + "sword-slash") with { Volume = 1f });
                            //should hit on this tick
                            break;
                        }
                    case 66:
                        {
                            Projectile.velocity = DashVelocity;
                            break;
                        }
                    case 67:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 68:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 69:
                        {
                            Projectile.velocity = DashVelocity;
                            Projectile.tileCollide = true;
                            break;
                        }
                    case 70:
                        {
                            Projectile.velocity = Falling;
                            KillTrail();
                            Projectile.tileCollide = true;
                            if (FallTimer < 80)
                            {
                                FallTimer++;
                            }
                            RandomMovementSet = false;
                            ShouldRotate = false;
                            //Dust.NewDust(CircleVector + targetCenter, 10, 10, DustID.Cloud);
                            CanHit = false;
                            break;
                        }
                    case int Finishing when (Finishing >= 71 && Finishing < 160):
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            if (Finishing > 100)
                            {
                            }
                            if (FallTimer < 100)
                            {
                                FallTimer++;
                            }
                            break;
                        }
                    case 160:
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            FallTimer = 0;
                            IsSlashing = false;
                            SlashProgress = 0;
                            ThrustStacks = 0;
                            AttackHit = false;
                            KillTrail();
                            break;
                        }
                    case >160:
                        {
                            Projectile.velocity = Falling;
                            Projectile.tileCollide = true;
                            FallTimer = 0;
                            IsSlashing = false;
                            SlashProgress = 0;
                            ThrustStacks = 0;
                            AttackHit = false;
                            break;
                        }
                }
            }
        }
        public bool AttackHit = false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsThrusting && ThrustStacks < 3 && !AttackHit)
            {
                ThrustStacks++;
            }
            if (IsSlashing && !SlashHit && ActualTarget == target)
            {
                Vector2 LightningPosition = target.Center + new Vector2(0, -1000);
                SlashHit = true;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile LightningStrike = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), LightningPosition, LightningPosition.DirectionTo(target.Center), ModContent.ProjectileType<SamuraiBeetleLightning>(), Projectile.damage, 0, Projectile.owner);
                }
            }
            AttackHit = true; //so it can't gain multiple thrust stacks by hitting multiple enemies at once
        }
        private void SpawnTrail()
        {
            Vector2 Position = Projectile.Center;
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile Trail = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Position, Projectile.velocity, ModContent.ProjectileType<SamuraiBeetleTrail>(), 0, 0, Projectile.owner, Projectile.whoAmI);
                Projectile.ai[2] = Trail.whoAmI;
            }
        }
        private void KillTrail()
        {
            if (Main.projectile[(int)Projectile.ai[2]].type == ModContent.ProjectileType<SamuraiBeetleTrail>())
            {
                Main.projectile[(int)Projectile.ai[2]].Kill();
            }
        }
        public int FlightTimer = 0;
        private void Visuals(bool foundTarget, float attackRange, Vector2 targetCenter, bool IsFlying)
        {
            if (!foundTarget)
            {
                if (ThrustProgress > 0)
                {
                    ThrustProgress++;
                    if (ThrustProgress >= 120)
                    {
                        KillTrail();
                        ThrustProgress = 0;
                        IsThrusting = false;
                    }
                }
                if (SlashProgress > 0)
                {
                    SlashProgress++;
                    if (SlashProgress >= 160)
                    {
                        KillTrail();
                        SlashProgress = 0;
                        IsSlashing = false;
                    }
                }
                if (IsFlying)
                {
                    int FlightFrameDuration = 4;
                    Projectile.frame = ANIM_FLYING_BEGIN - 1 + FlightTimer / FlightFrameDuration;
                    if (FlightTimer >= ANIM_FLYING_FRAMES * FlightFrameDuration)
                    {
                        FlightTimer = 0;
                    }
                    else
                    {
                        FlightTimer++;
                    }
                }
            }
            if (foundTarget)
            {
                Projectile.frame = 0;
            }
            if (ThrustProgress > 0)
            {
                switch (ThrustProgress)
                {
                    case int JumpFrames when JumpFrames >= 0 && JumpFrames <= 22:
                        {
                            Projectile.frame = ANIM_JUMP_BEGIN - 1 + 4 - (JumpFrames / 8); //first frame
                            break;
                        }
                    case int FirstFrame when (FirstFrame >= 22 && FirstFrame <= 30):
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 0; //first frame
                            break;
                        }
                    case int SecondFrame when (SecondFrame > 30 && SecondFrame <= 38):
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 1; //second frame
                            break;
                        }
                    case int ThirdFrame when ThirdFrame > 38 && ThirdFrame <= 44:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 2; //and so on
                            break;
                        }
                    case int FourthFrame when FourthFrame > 44 && FourthFrame <= 59:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 3;
                            break;
                        }
                    case int FifthFrame when FifthFrame > 59 && FifthFrame <= 74:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 4;
                            break;
                        }
                    case int SixthFrame when SixthFrame > 74 && SixthFrame <= 89:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 5;
                            break;
                        }
                    case int SeventhFrame when SeventhFrame > 89 && SeventhFrame <= 104:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 6;
                            break;
                        }
                    case int EighthFrame when EighthFrame > 104 && EighthFrame <= 120:
                        {
                            Projectile.frame = ANIM_THRUST_BEGIN - 1 + 7;
                            break;
                        }
                }
            }
            if (SlashProgress > 0)
            {
                switch (SlashProgress)
                {
                    case int JumpFrames when JumpFrames >= 0 && JumpFrames <= 22:
                        {
                            Projectile.frame = ANIM_JUMP_BEGIN - 1 + 4 - (JumpFrames / 8); //first frame
                            break;
                        }
                    case int FirstFrame when (FirstFrame >= 0 && FirstFrame <= 19):
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 0; //first frame
                            break;
                        }
                    case int SecondFrame when (SecondFrame > 19 && SecondFrame <= 39):
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 1; //second frame
                            break;
                        }
                    case int ThirdFrame when ThirdFrame > 39 && ThirdFrame <= 59:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 2; //and so on
                            break;
                        }
                    case int FourthFrame when FourthFrame > 59 && FourthFrame <= 79:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 3;
                            break;
                        }
                    case int FifthFrame when FifthFrame > 79 && FifthFrame <= 99:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 4;
                            break;
                        }
                    case int SixthFrame when SixthFrame > 99 && SixthFrame <= 119:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 5;
                            SlashHit = false;
                            break;
                        }
                    case int SeventhFrame when SeventhFrame > 119 && SeventhFrame <= 139:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 6;
                            break;
                        }
                    case int EighthFrame when EighthFrame > 139 && EighthFrame <= 160:
                        {
                            Projectile.frame = ANIM_SLASH_BEGIN - 1 + 7;
                            break;
                        }
                }
                if (SlashHit && Projectile.frame == ANIM_SLASH_BEGIN - 1 + 3)
                {
                    Projectile.frame++;
                }
            }
            if (ShouldRotate)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Projectile.spriteDirection == -1)
                {
                    Projectile.rotation -= (float)Math.PI;
                }
            }
            else
            {
                Projectile.rotation = 0;
            }
        }
    }
}