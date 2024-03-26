using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.NPCs;
using static Humanizer.In;

namespace tsorcRevamp.Projectiles.Summon.ForgottenImp
{
    public class ForgottenImpProjectile : ModdedMinionProjectile
    {
        public override int ProjectileFrames => 12;
        public override int Width => 66;
        public override int Height => 52;
        public override float MinionSlotsRequired => ForgottenImpHalberd.RequiredSlots;
        public override int ProjectileBuffType => ModContent.BuffType<ForgottenImpBuff>();
        public override bool ContactDamage => throw new NotImplementedException();
        public override DamageClass ProjectileDamageType => DamageClass.Summon;
        public override float SummonTagDamageMultiplier => ForgottenImpHalberd.SummonTagDmgMult;
        public override int ShotProjectileType => ModContent.ProjectileType<ForgottenImpDagger>();
        public override void CustomSetStaticDefaults()
        {
        }
        public override void CustomSetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Hemorrhage>(), ForgottenImpHalberd.BleedDuration * 60);
            target.AddBuff(ModContent.BuffType<Hemorrhage>(), ForgottenImpHalberd.BleedDuration * 60);
        }
        public override bool MinionContactDamage()
        {
            if (Projectile.frame == 6)
            {
                return true;
            }
            return false;
        }
        public override void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
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
        public override void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter)
        {
            // Starting search distance
            distanceFromTarget = 700f;
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
                        bool closeThroughWall = between < 100f;

                        if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall))
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
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox.Inflate(hitbox.Width, hitbox.Height);
            base.ModifyDamageHitbox(ref hitbox);
        }
        public override void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
        {
            // Default movement parameters (here for attacking)
            float speed = 8f;
            float inertia = 20f;
            Projectile.ai[1]--;

            if (foundTarget)
            {
                // Minion has a target: attack (here, fly towards the enemy)
                if (distanceFromTarget > 20f)
                {
                    Vector2 direction = targetCenter - Projectile.Center;
                    direction.Normalize();
                    direction *= speed;

                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
                }
                if (!Projectile.Hitbox.Contains(targetCenter.ToPoint()))
                {
                    Projectile.ai[2]--;
                    if (Projectile.ai[1] <= 0)
                    {
                        Projectile.ai[1] = FrameDuration * 4;
                        Projectile.ai[0] = 2; //switch to throwing attack mode
                    }
                    if (Projectile.frame == 10 && Projectile.ai[2] < 0) //throw a dagger if conditions are met
                    {
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.DirectionTo(targetCenter) * 4, ShotProjectileType, Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                        Projectile.ai[2] = FrameDuration + 2;
                    }
                }
                else 
                {
                    if (Projectile.ai[1] <= 0)
                    {
                        Projectile.ai[1] = FrameDuration * 4;
                        Projectile.ai[0] = 1; //switch to stab mode if conditions are met
                    }
                }
            }
            else
            {
                // Minion doesn't have a target: return to player and idle
                Projectile.ai[0] = 0;
                if (distanceToIdlePosition > 600f)
                {
                    // Speed up the minion if it's away from the player
                    speed = 12f;
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
            }
        }
        public int FrameDuration = 5;
        public override void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.05f;

            // This is a simple "loop through all frames from top to bottom" animation

            switch (Projectile.ai[0])
            {
                case 0:
                    {
                        Projectile.frameCounter++;

                        if (Projectile.frameCounter >= FrameDuration)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame++;

                            if (Projectile.frame >= 4)
                            {
                                Projectile.frame = 0;
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        Projectile.frame = 3 + (int)((Projectile.ai[1] + 4) / FrameDuration);
                        break;
                    }
                case 2:
                    {
                        Projectile.frame = 7 + (int)((Projectile.ai[1] + 4) / FrameDuration);
                        break;
                    }
            }
            Projectile.spriteDirection = Projectile.direction;

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
        }
    }
}