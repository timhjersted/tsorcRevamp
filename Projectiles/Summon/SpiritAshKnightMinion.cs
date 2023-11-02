using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon
{
    class SpiritAshKnightMinion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;

            Main.projPet[Type] = true;

            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }
        public sealed override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 98;
            Projectile.tileCollide = false; // Makes the minion go through tiles freely
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = AttackFrameSpeed * 2;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
        public float NPCCollisionHitboxMultiplier = 1.5f;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.frame > 6 && new Rectangle(projHitbox.X, projHitbox.Y, (int)(projHitbox.Width * NPCCollisionHitboxMultiplier), (int)(projHitbox.Height * NPCCollisionHitboxMultiplier)).Intersects(targetHitbox))
            { //it can deal damage during the last 2 frames of it's attack animation and at a multiplier of it's hitbox size, it tends to miss a lot otherwise since it always keeps moving
                return true;
            }
            return false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!CheckActive(player))
            {
                return;
            }

            TileCollisionCooldown--;
            Idle(player, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
            SearchForTargets(player, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            Movement(foundTarget, distanceFromTarget, targetCenter, distanceToIdlePosition, vectorToIdlePosition);
            Visuals(distanceFromTarget, targetCenter);
        }
        // This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<SpiritAshKnightBuff>());

                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<SpiritAshKnightBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }

        private void Idle(Player player, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
        {
            Vector2 idlePosition = player.Center;
            idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

            // If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
            // The index is projectile.minionPos
            float minionPositionOffsetX = (10 + Projectile.minionPos * 40) * -player.direction;
            idlePosition.X += minionPositionOffsetX; // Go behind the player

            // All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

            // Teleport to player if distance is too big
            vectorToIdlePosition = idlePosition - Projectile.Center;
            distanceToIdlePosition = vectorToIdlePosition.Length();

            if (Main.myPlayer == player.whoAmI && distanceToIdlePosition > 2000f)
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
            distanceFromTarget = 1000f;
            targetCenter = Projectile.position;
            foundTarget = false;

            // This code is required if your minion weapon has the targeting feature
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[owner.MinionAttackTargetNPC];
                float between = Vector2.Distance(npc.Center, Projectile.Center);

                // Reasonable distance away so it doesn't target across multiple screens
                if (between < 1400f)
                {
                    distanceFromTarget = between;
                    targetCenter = npc.Center;
                    foundTarget = true;
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
                        bool closeThroughWall = between < distanceFromTarget;

                        if (((closest && inRange) || (!foundTarget && inRange)) && (lineOfSight || closeThroughWall))

                        {
                            distanceFromTarget = between;
                            targetCenter = npc.Center;
                            foundTarget = true;
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
        public Vector2 DashVelocity;
        public float DashSpeed = 9.5f;
        public int TileCollisionCooldown = 0;
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[2] >= 1) //if it started attacking and hasn't bounced off a tile in the last few ticks
            {
                SearchForTargets(player, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter); //get target
                DashVelocity = UsefulFunctions.Aim(Projectile.Center, targetCenter, DashSpeed); //set velocity to dash towards the target
                Projectile.ai[2] = 2; //signal the ai that it has bounced off the first block
                MissedAttackDelay = 0; //resetting this timer because it didn't miss
                TileCollisionCooldown = 15; //setting a cooldown on how often it can reset it's velocity on tile collision 
                Projectile.tileCollide = false; //so it can't bounce off a tile in the next few ticks and reach enemies consistently
            }
            return false;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().BaseSummonTagCriticalStrikeChance += SpiritBell.BaseCritChance;
            modifiers.CritDamage *= 1f + SpiritBell.CritDmg / 100f;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TileCollisionCooldown = 0; //so it can readjust it's movement by bouncing off tiles again after having hit an enemy
            MissedAttackDelay = 0; //it didn't miss
        }
        public int MissedAttackDelay = 0;
        public float DiveSpeed = 0.2f;
        private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
        {
            // Default movement parameters
            float speed;
            float inertia;
            if (foundTarget && distanceToIdlePosition < 1000f)
            {
                if (TileCollisionCooldown < 0)
                {
                    Projectile.tileCollide = true;
                }
                switch (Projectile.ai[2])
                {
                    case 0:
                        {
                            Projectile.ai[2] = 1; //signal the ai that it should start attacking
                            Projectile.velocity.X = 0;
                            Projectile.velocity.Y = DiveSpeed; //dive towards the ground, check ontilecollide for next step
                            TileCollisionCooldown = 0; //reset
                            break;
                        }
                    case 1:
                        {
                            Projectile.velocity.X = 0;
                            Projectile.velocity.Y += DiveSpeed; //dive towards the ground, check ontilecollide for next step
                            break;
                        }
                    case 2:
                        {
                            Projectile.velocity = DashVelocity;
                            MissedAttackDelay++;
                            if (MissedAttackDelay > 90) //1.5 second timer that runs after it starts to dash so that it won't keep floating forever if it misses
                            {
                                Projectile.ai[2] = 0;
                            }
                            break;
                        }
                }
            }
            else
            {
                Projectile.ai[2] = 0;
                Projectile.tileCollide = false;
                #region Idle stuff
                // Minion doesn't have a target: return to player and idle
                if (distanceToIdlePosition > 100f)
                {
                    // Speed up the minion if it's away from the player
                    speed = 24f;
                    inertia = 60f;
                }
                else
                {
                    // Slow down the minion if closer to the player
                    speed = 8f;
                    inertia = 80f;
                }

                if (distanceToIdlePosition > 20f)
                {
                    // The immediate range around the player (when it passively floats about)

                    // This is a simple movement formula using the two parameters and its desired direction to create a "homing" movement
                    vectorToIdlePosition.Normalize();
                    vectorToIdlePosition *= speed;
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToIdlePosition) / inertia;
                }
                else if (Projectile.velocity == Vector2.Zero)
                {
                    // If there is a case where it's not moving at all, give it a little "poke"
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
                #endregion
            }
        }
        public int AttackTimer = 0;
        public int FrameSpeed = 5;
        public int AttackFrameSpeed = 6;
        public int FirstAttackFrame = 5;
        private void Visuals(float distanceFromTarget, Vector2 targetCenter)
        {
            if (Projectile.velocity.X > 0.05)
            {
                Projectile.spriteDirection = 1;
            }
            if (Projectile.velocity.X < -0.05)
            {
                Projectile.spriteDirection = -1;
            }

            //idle animation
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type] - FirstAttackFrame)
                {
                    Projectile.frame = 0;
                }
            }

            //attack animation
            if (distanceFromTarget < 150f) //starts attack animation once relatively close to the target
            {
                AttackTimer++;
            } 
            else 
            {
                AttackTimer = 0; //resets attack animation if there is no target
            }
            if (AttackTimer > 0)
            {
                Projectile.frame = (AttackTimer / AttackFrameSpeed) + (FirstAttackFrame - 1); //sets frame, frame number starts at 0 for the first frame so it needs to be decreased by 1
            }
            if (AttackTimer > AttackFrameSpeed * 4) //loops to the start of the attack animation
            {
                AttackTimer = 0;
            }
        }
    }
}
