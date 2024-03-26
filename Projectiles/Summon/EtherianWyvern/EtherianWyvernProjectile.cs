using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.NPCs;
using static Humanizer.In;

namespace tsorcRevamp.Projectiles.Summon.EtherianWyvern
{
    public class EtherianWyvernProjectile : ModdedMinionProjectile
    {
        public override int ProjectileFrames => 7;
        public override int Width => 68;
        public override int Height => 50;
        public override float MinionSlotsRequired => EtherianWyvernStaff.SlotsRequired;
        public override int ProjectileBuffType => ModContent.BuffType<EtherianWyvernBuff>();
        public override bool ContactDamage => true;
        public override DamageClass ProjectileDamageType => DamageClass.Summon;
        public override float SummonTagDamageMultiplier => EtherianWyvernStaff.SummonTagDmgMult;
        public override int ShotProjectileType => ModContent.ProjectileType<EtherianWyvernFireball>();
        public int AttackSpeed = 60; //lower is better
        public override void CustomSetStaticDefaults()
        {
        }
        public override void CustomSetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if(Condition.DownedOldOnesArmyT3.IsMet())
            {
                target.AddBuff(BuffID.BetsysCurse, EtherianWyvernStaff.DebuffDuration * 60);
            }
        }
        public override void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
        {
            // Default movement parameters (here for attacking)
            float speed = 16f;
            float inertia = 20f;
            float fireballSpeed = 7f;
            float fireballSpread = 15f;

            if (foundTarget)
            {
                Projectile.ai[0]++;
                // Minion has a target: attack (here, fly towards the enemy)
                if (distanceFromTarget > 200f)
                {
                    Projectile.ai[1] = 1;
                    // The immediate range around the target (so it doesn't latch onto it when close)
                    Vector2 direction = targetCenter - Projectile.Center;
                    direction.Normalize();
                    direction *= speed;

                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
                }
                else
                {
                    Projectile.ai[1] = 0;
                    if (Main.myPlayer == Projectile.owner && Projectile.ai[0] >= 60)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.DirectionTo(targetCenter + new Vector2(0, -fireballSpread)) * fireballSpeed, ShotProjectileType, Projectile.damage, Projectile.knockBack, Projectile.owner);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.DirectionTo(targetCenter) * fireballSpeed, ShotProjectileType, Projectile.damage, Projectile.knockBack, Projectile.owner);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.DirectionTo(targetCenter + new Vector2(0, fireballSpread)) * fireballSpeed, ShotProjectileType, Projectile.damage, Projectile.knockBack, Projectile.owner);
                        Projectile.ai[0] = 0;
                        Projectile.netUpdate = true;
                    }
                }
            }
            else
            {
                Projectile.ai[1] = 1;
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
        }
        public int FrameDuration = 5;
        public override void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.05f;

            // This is a simple "loop through all frames from top to bottom" animation

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= FrameDuration)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= 5)
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.ai[1] == 0 && Projectile.ai[0] > 25)
            {
                if (Projectile.ai[0] <= 45)
                {
                    Projectile.frame = 5;
                }
                else 
                { 
                    Projectile.frame = 6; 
                }
            }
            Projectile.spriteDirection = Projectile.direction;

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
        }
    }
}