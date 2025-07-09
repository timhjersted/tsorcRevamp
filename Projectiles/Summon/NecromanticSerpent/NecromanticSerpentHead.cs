using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Summon.NecromanticSerpent
{
    public class NecromanticSerpentHead : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;

            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = false;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }
        public static DamageClass DamageType => DamageClass.Magic;
        public Projectile tail => body[body.Length - 1];
        public Projectile[] body;
        public float[] oldRotations;
        public Player player => Main.player[Projectile.owner];
        public static float OriginalDamage => 60f;
        public static float OriginalKnockback => 2f;
        public static int BuffType => ModContent.BuffType<NecromanticSerpentBuff>();
        public static int BodyType => ModContent.ProjectileType<NecromanticSerpentBody>();
        public static int TailType => ModContent.ProjectileType<NecromanticSerpentTail>();
        public int tickCount = 0;
        public int TicksBeforeUpdate = 1;
        public int bodyLength = 20;
        public static int NPCHitCooldown => 15;
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageType;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.originalDamage = (int)OriginalDamage;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = NPCHitCooldown;
        }
        public override bool PreAI()
        {
            if (!Active()) return false;

            bool onClient = Main.netMode == NetmodeID.MultiplayerClient;

            if (body == null)
            {
                if (onClient) return false;

                SpawnBody();
            }

            PreUpdateBody();

            return true;
        }

        public override void AI()
        {
            Movement();


        }

        public void Movement()
        {
            NPC target = PrioritizeTarget(1000f, 750f);

            float speed = 25f;
            float inertia = 35f;

            if (target != null) // move to attack the target
            {
                if (Projectile.position.DistanceSQ(target.position) > 120f)
                {
                    // The immediate range around the target (so it doesn't latch onto it when close)
                    Vector2 direction = target.Center - Projectile.Center;
                    direction.Normalize();
                    direction *= speed;

                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
                }
            }
            else
            {
                Vector2 randomOffset = new Vector2(Main.rand.Next(-40, 41), Main.rand.Next(-10, 11));
                Vector2 idlePosition = new Vector2(player.position.X, player.position.Y - 48) + randomOffset;

                Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
                float distanceToIdlePosition = vectorToIdlePosition.Length();

                if (Main.netMode != NetmodeID.Server && distanceToIdlePosition > 2000f)
                {
                    // Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
                    // and then set netUpdate to true
                    Projectile.position = idlePosition;
                    Projectile.velocity *= 0.1f;
                    Projectile.netUpdate = true;
                }

                // Minion doesn't have a target: return to player and idle
                if (distanceToIdlePosition > 600f)
                {
                    // Speed up the minion if it's away from the player
                    speed *= 1.5f;
                    inertia *= 3f;
                }
                else
                {
                    // Slow down the minion if closer to the player
                    speed *= 1f;
                    inertia *= 4f;
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

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Speed up as you hit the enemy so you run through it
            Projectile.velocity *= 1.8f; 

            //50% to heal 1 hp
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(2)) 
            {
                player.statLife += 1;
                player.HealEffect(1); 
            }   
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
        public bool Active()
        {
            var modPlayer = UsefulFunctions.ModPlayer(player);

            if (modPlayer.NecromanticSerpent && !player.dead)
            {
                Projectile.timeLeft = 2;

                if (body != null)
                {
                    foreach (Projectile segment in body)
                    {
                        segment.timeLeft = 2;
                    }
                }

                return true;
            }
            else
            {
                body = null;
                return false;
            }
        }
        public void SpawnBody()
        {
            tickCount = 0;

            body = new Projectile[bodyLength];
            for (int i = 0; i < bodyLength - 1; i++)
            {
                int bodyPos = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, BodyType, (int)OriginalDamage / 2, OriginalKnockback, Projectile.owner);
                body[i] = Main.projectile[bodyPos];
                body[i].originalDamage = Projectile.originalDamage;
            }

            int tailPos = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, TailType, (int)OriginalDamage / 2, OriginalKnockback, Projectile.owner);
            body[body.Length - 1] = Main.projectile[tailPos];
            tail.originalDamage = Projectile.originalDamage;
        }

        public static void UpdateSegmentPos(Projectile from, Projectile to)
        {
            float minimumDistance = 18f;
            minimumDistance *= minimumDistance;

            if (to.position.DistanceSQ(from.position) < minimumDistance)
            {
                from.velocity *= 0.1f;
            }
            else
            {
                from.position.X = to.position.X;
                from.position.Y = to.position.Y;
                from.rotation = to.rotation;
            }

            from.velocity += (to.position - from.position) * 0.1f;
        }

        // Moves body segments to the position of the segment before it every 'TicksBeforeUpdate' ticks.
        public void PreUpdateBody()
        {
            tickCount++;

            if (tickCount != TicksBeforeUpdate) return;
            
            tickCount = 0;

            for (int i = body.Length - 1; i > 0; i--)
            {
                Projectile from = body[i];
                Projectile to = body[i - 1];

                Lighting.AddLight((from.Center + to.Center) / 2f, TorchID.Demon);

                if (Main.rand.NextFloat() < 0.8f) 
                {
                    int pos = Dust.NewDust((from.Center + to.Center) / 2f, 20, 10, DustID.Crimson, from.velocity.X, from.velocity.Y, Alpha: 10, Scale: 1.4f);
                    Main.dust[pos].noGravity = true;
                }

                UpdateSegmentPos(from, to);
            }

            float xOffset = 0;
            float yOffset = 13;

            UpdateSegmentPos(body[0], Projectile);

            // Set the neck segment to that of the head.
            body[0].position.X = Projectile.position.X + xOffset;
            body[0].position.Y = Projectile.position.Y + yOffset;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }

        public NPC PrioritizeTarget(float MaxDistanceFromPlayer, float MaxDistanceFromProj)
        {
            NPC target = null;
            var modPlayer = UsefulFunctions.ModPlayer(player);
            NPC lastAttackedNPC = null;

            // Squaring it to account for negative distances
            MaxDistanceFromPlayer *= MaxDistanceFromPlayer;
            MaxDistanceFromProj *= MaxDistanceFromProj;

            if (player.HasMinionAttackTargetNPC)
            {
                target = Main.npc[player.MinionAttackTargetNPC];
                return target;
            }
            else if (modPlayer.LastAttackedNPCIndex != -1)
                lastAttackedNPC = Main.npc[modPlayer.LastAttackedNPCIndex];

            foreach (NPC npc in Main.ActiveNPCs)
            {
                // Discard it if the NPC is not chaseable
                if (!npc.CanBeChasedBy()) continue;
                // Discard it if it's too far away from the player
                if (npc.position.DistanceSQ(player.position) > MaxDistanceFromPlayer) continue;

                float distanceFromProj = Projectile.position.DistanceSQ(npc.position);
                if (distanceFromProj < MaxDistanceFromProj)
                {
                    // Constantly update the closest found distance from npc to projectile and set that as the next prospective target
                    MaxDistanceFromProj = distanceFromProj;
                    target = npc;
                }
            }

            if (lastAttackedNPC != null && lastAttackedNPC.active)
            {
                float lastAttackedDistanceFromProj = Projectile.position.DistanceSQ(lastAttackedNPC.position);

                // Prioritize the target the player attacked if its less than 2 times the distance away from the actual closest target
                if (lastAttackedDistanceFromProj < MaxDistanceFromProj * 2)
                    target = lastAttackedNPC;
            }

            return target;
        }
    }
}
