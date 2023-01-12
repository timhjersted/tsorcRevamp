using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra;
using tsorcRevamp.Projectiles.Trails;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
    public class CenterOfTheUniverseStar : ModProjectile
    {
        public float angularSpeed3 = 0.03f;
        public static float circleRad3 = 50f;
        public float currentAngle3 = 0;
        public static int timer3 = 0;
        bool spawnedTrail = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }
        public sealed override void SetDefaults()
        {
            Projectile.width = 98;
            Projectile.height = 54;
            Projectile.tileCollide = false; // Makes the minion go through tiles freely

            // These below are needed for a minion weapon
            Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
            Projectile.minion = true; // Declares this as a minion (has many effects)
            Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
            Projectile.minionSlots = 1f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
            Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles
            Projectile.extraUpdates = 1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                damage += (Projectile.damage / 4);
            }
        }
        public override void OnSpawn(IEntitySource source)
        {
            CenterOfTheUniverse.projectiles.Add(this);
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
        public override void Kill(int timeLeft)
        {
            CenterOfTheUniverse.projectiles.Remove(this);
        }
        Vector2 lastPos;
        CenterOfTheUniverseTrail trail;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            Vector2 visualplayercenter = owner.Center + new Vector2(-27, -12);

            if (!CheckActive(owner))
            {
                return;
            }

            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                angularSpeed3 = 0.075f;
                owner.statMana -= 1;
                owner.manaRegenDelay = 10;
            }
            if (!owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost || (owner.statMana <= 0) || owner.HasBuff(BuffID.ManaSickness))
            {
                angularSpeed3 = 0.03f;
                owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = false;
            }

            currentAngle3 += (angularSpeed3 / (circleRad3 * 0.001f + 1f));

            Vector2 offset = new Vector2(MathF.Sin(currentAngle3), MathF.Cos(currentAngle3)) * circleRad3;

            Projectile.position = visualplayercenter + offset;
            if (!spawnedTrail)
            {
                trail = (CenterOfTheUniverseTrail)Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<CenterOfTheUniverseTrail>(), 0, 0, Projectile.owner, 0, Projectile.whoAmI).ModProjectile;
                spawnedTrail = true;
            }

            Visuals();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < Projectile.height * 1.2f && distance > Projectile.height * 1.2f - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());

                return false;
            }

            if (!owner.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>()))
            {
                circleRad3 = 50f;
                currentAngle3 = 0;
                CenterOfTheUniverse.projectiles.Clear();
            }

            if (owner.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (crit)
            {
                Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().CritCounter += 1;
                target.AddBuff(ModContent.BuffType<SunburnDebuff>(), 80);
            }
            else
            {
                target.AddBuff(ModContent.BuffType<SunburnDebuff>(), 40);
            }
        }
        private void Visuals()
        {

            Projectile.rotation = currentAngle3 * -1f;

            float frameSpeed = 3f;

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
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.48f);
        }
    }
}