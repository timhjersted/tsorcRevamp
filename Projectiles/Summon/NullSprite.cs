using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace tsorcRevamp.Projectiles.Summon
{
    public class NullSprite : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Null Sprite");
            Main.projFrames[Projectile.type] = 1; //4?
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 0.75f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active)
            {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            Lighting.AddLight(Projectile.Center, 0.45f, 0, 0.3f);
            AI_062();
        }

        private void AI_062() {
            int attackInterval = 60;
            Projectile.tileCollide = true;
            float idleSpeed = 0.6f;
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile p = Main.projectile[i];
                //if there are other null sprites
                if (i != Projectile.whoAmI && p.active && p.owner == Projectile.owner && p.type == Projectile.type) {
                    //and theyre too close
                    if (Vector2.Distance(Projectile.Center, p.Center) < Projectile.width) {
                        Projectile.velocity.X += idleSpeed * Math.Sign(Projectile.position.X - Projectile.position.X);
                        Projectile.velocity.X += idleSpeed * Math.Sign(Projectile.position.Y - Projectile.position.Y);
                    }
                }
            }

            Vector2 targetLocation = Projectile.position;
            bool foundTarget = false;
            float targetDistance = 2000f;

            NPC target = Projectile.OwnerMinionAttackTargetNPC;
            if (target != null && target.CanBeChasedBy(this)) {
                float checkDistance = Vector2.Distance(target.Center, Projectile.Center);
                if (Collision.CanHitLine(Projectile.Center, Projectile.width, Projectile.height, target.Center, target.width, target.height)) {
                    targetDistance = checkDistance;
                    targetLocation = target.Center;
                    foundTarget = true;
                }
            }

            if (!foundTarget) {
                for (int i = 0; i < 200; i++) {
                    NPC checkNPC = Main.npc[i];
                    if (!checkNPC.CanBeChasedBy(this))
                        continue;
                    
                    float distance = Vector2.Distance(checkNPC.Center, Projectile.Center);
                    if ((distance < targetDistance) && Collision.CanHitLine(Projectile.Center, 1, 1, checkNPC.Center, 1, 1)) {
                        targetDistance = distance;
                        targetLocation = checkNPC.Center;
                        foundTarget = true;
                    }
                    
                } 
            }

            int leashRange = 600;
            if (foundTarget)
                leashRange = 1200;
            Player owner = Main.player[Projectile.owner];
            if (Vector2.Distance(owner.Center, Projectile.Center) > leashRange) {
                Projectile.ai[0] = 1;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 1)
                Projectile.tileCollide = false;


            int minionOffset = 1;
            for (int i = 0; i < Projectile.whoAmI; i++) {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == Projectile.type) {
                    minionOffset++;
                }
            }

            if (foundTarget && Projectile.ai[0] == 0) {
                Vector2 toTarget = targetLocation - Projectile.Center;
                float toTargetLength = toTarget.Length();
                toTarget.Normalize();
                if (toTargetLength > 350) {
                    float scalar = 8;
                    toTarget *= scalar;
                    Projectile.velocity.X = (Projectile.velocity.X * 40f + toTarget.X) / 41f;
                    Projectile.velocity.Y = (Projectile.velocity.Y * 40f + toTarget.Y) / 41f;
                }
                else {
                    if (toTargetLength < 325) {
                        float reverseScalar = 5f;
                        toTarget *= reverseScalar;
                        Projectile.velocity.X = (Projectile.velocity.X * 40f + toTarget.X) / 41f;
                        Projectile.velocity.Y = (Projectile.velocity.Y * 40f + toTarget.Y) / 41f;
                    }
                    else {
                        Projectile.velocity *= 0.85f;
                    }
                }
            }
            else {
                if (!Collision.CanHitLine(Projectile.Center, 1, 1, owner.Center, 1, 1)) {
                    Projectile.ai[0] = 1;
                }
                float returnStrength = 12f;
                if (Projectile.ai[0] == 1) {
                    returnStrength = 20f;
                }
                Vector2 toOwner = owner.Center - Projectile.Center;
                Projectile.netUpdate = true;
                float toOwnerLength = toOwner.Length();

                toOwner.X -= 64 * owner.direction;
                toOwner.Y -= 70;

                float rotation = MathHelper.ToRadians(Main.GameUpdateCount % (60 + (3 * (minionOffset + 3))));
                toOwner += new Vector2(0, 12 + (2 * (minionOffset + 3))).RotatedBy(rotation * owner.direction);

                if (toOwnerLength < 600f && Projectile.ai[0] == 1f && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }

                if (toOwnerLength > 2000f) {
                    Projectile.Center = owner.Center;
                }

                toOwner.Normalize();
                if (toOwnerLength < 320f) {
                    returnStrength /= 2f;
                }
                toOwner *= returnStrength;
                Projectile.velocity = (Projectile.velocity * 40f + toOwner) / 41f;
                
            }
            if (Main.rand.NextBool(4)) {
                Dust dust = Dust.NewDustDirect(new Vector2(Projectile.position.X - Projectile.width, Projectile.position.Y - Projectile.height), Projectile.width * 2, Projectile.height * 2, DustID.Shadowflame, -Projectile.velocity.X, -Projectile.velocity.Y, 100, default, 1.5f);
                dust.noGravity = true;
                dust.noLight = true;
            }
            if (Projectile.ai[1] < attackInterval) {
                Projectile.ai[1] += 1;
            }

            if (Projectile.ai[0] != 0 || !foundTarget)
                return;
            int shotType = ModContent.ProjectileType<SummonProjectiles.NullSpriteBeam>();
            float shotSpeed = 3f;
            if (Projectile.ai[1] >= attackInterval) {
                Projectile.ai[1] = 0f;
                Vector2 shotVector = targetLocation - Projectile.Center;
                shotVector.Normalize();
                shotVector *= shotSpeed;
                if (Main.myPlayer == Projectile.owner) {
                    int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, shotVector, shotType, Projectile.damage, 0f, Main.myPlayer);
                    Main.projectile[p].originalDamage = Projectile.damage;
                    Main.projectile[p].timeLeft = 600;
                    Main.projectile[p].netUpdate = true;
                    Projectile.netUpdate = true;
                }
                Projectile.velocity = (-shotVector * 1.75f).RotatedByRandom(MathHelper.ToRadians(25));
                Projectile.netUpdate = true;
            }
        }
    }
}
