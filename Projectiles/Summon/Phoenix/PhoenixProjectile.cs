using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.Phoenix
{
    public class PhoenixProjectile : ModProjectile
    {
        public int WarmupStacksFallOffTimer = 0;
        public int WarmupStacksTimer = 0;
        public int WarmupStacks = 0;
        public int BaseAttackSpeedCooldown = 37; //Lower is better

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
            // This is necessary for right-click targeting
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion

            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.

            ProjectileID.Sets.SummonTagDamageMultiplier[Projectile.type] = PhoenixEgg.SummonTagDmgMult / 100f;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 104;
            Projectile.height = 93;
            Projectile.tileCollide = false; // Makes the minion go through tiles freely

            // These below are needed for a minion weapon
            Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
            Projectile.minion = true; // Declares this as a minion (has many effects)
            Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
            Projectile.minionSlots = 2f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
            Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BaseAttackSpeedCooldown;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            WarmupStacksFallOffTimer = 0;
            WarmupStacksTimer = 0;
            if (WarmupStacks < PhoenixEgg.MaxStacks - 1)
            {
                WarmupStacks++;
            }
            if (hit.Crit)
            {
                Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ModContent.ProjectileType<PhoenixBoomCrit>(), 0, 0);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1f + (((float)WarmupStacks - PhoenixEgg.MinStacks) / PhoenixEgg.DmgDivisor);
            modifiers.CritDamage += PhoenixEgg.CritDamage / 100f;
        }

        // Here you can decide if your minion breaks things like grass or pots
        public override bool? CanCutTiles()
        {
            return false;
        }

        // This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
        public override bool MinionContactDamage()
        {
            return true;
        }

        // The AI of this minion is split into multiple methods to avoid bloat. This method just passes values between calls actual parts of the AI.
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!CheckActive(player))
            {
                return;
            }

            GeneralBehavior(player, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
            SearchForTargets(player, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            Movement(foundTarget, distanceFromTarget, targetCenter, distanceToIdlePosition, vectorToIdlePosition);
            Visuals();

            switch (WarmupStacks)
            {
                case int Tier1 when (Tier1 <= 10 && Tier1 >= 5):
                    {
                        Dust.NewDust(Projectile.Center, 10, 10, DustID.GoldFlame, 0f, 0f, 150, Color.Yellow, 0.25f);
                        break;
                    }
                case int Tier2 when (Tier2 < 16 && Tier2 > 10):
                    {
                        Dust.NewDust(Projectile.Center, 20, 20, DustID.GoldFlame, 0f, 0f, 200, Color.Orange, 0.75f);
                        break;
                    }
                case int Tier3 when (Tier3 >= 16):
                    {
                        Dust.NewDust(Projectile.Center, 40, 40, DustID.GoldFlame, 0f, 0f, 250, Color.OrangeRed, 1f);
                        Dust.NewDust(Projectile.Center, 40, 40, DustID.SolarFlare, 0f, 0f, 250, default, 1f);
                        break;
                    }
            }
            if (Main.GameUpdateCount % 60 == 0)
            {
                WarmupStacksFallOffTimer++;
            }
            if (WarmupStacksFallOffTimer >= 5)
            {
                if (Main.GameUpdateCount % 60 == 0)
                {
                    WarmupStacksTimer++;
                }
                switch (WarmupStacksTimer)
                {
                    case 0:
                        {
                            break;
                        }
                    case 1:
                        {
                            WarmupStacks -= 1;
                            break;
                        }
                    case 2:
                        {
                            WarmupStacks -= 1;
                            break;
                        }
                    case 3:
                        {
                            WarmupStacks -= 2;
                            break;
                        }
                    case 4:
                        {
                            WarmupStacks -= 2;
                            break;
                        }
                    default:
                        {
                            WarmupStacks -= 3;
                            break;
                        }
                }
                if (WarmupStacks < 0)
                {
                    WarmupStacks = 0;
                }
            }
        }

        // This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<PhoenixBuff>());

                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<PhoenixBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }

        private void GeneralBehavior(Player player, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
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
                Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare, null, 0, Color.DarkOrange, 1);
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
            distanceFromTarget = 700f;
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
                        bool closeThroughWall = between < 700f;

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

        private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
        {
            // Default movement parameters (here for attacking)
            float speed = 10f * (1.3f + ((float)WarmupStacks / 80));
            float inertia = 20f;

            if (foundTarget)
            {
                // Minion has a target: attack (here, fly towards the enemy)
                if (distanceFromTarget > 10f)
                {
                    // The immediate range around the target (so it doesn't latch onto it when close)
                    Vector2 direction = targetCenter - Projectile.Center;
                    direction.Normalize();
                    direction *= speed;

                    if (Main.GameUpdateCount % (BaseAttackSpeedCooldown / (1 + WarmupStacks / 10)) == 0)
                    {
                        Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, targetCenter, speed * (1.3f + ((float)WarmupStacks / 80)));
                        Projectile.ResetLocalNPCHitImmunity();
                    }
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
                }
            }
            else
            {
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
            }
        }

        private void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.1f;

            if (Projectile.velocity.X > 0.05)
            {
                Projectile.spriteDirection = -1;
            }
            if (Projectile.velocity.X < -0.05)
            {
                Projectile.spriteDirection = 1;
            }

            // This is a simple "loop through all frames from top to bottom" animation
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        }
    }
}