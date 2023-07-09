using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using tsorcRevamp.Buffs.Summon;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class TerraFallTerraprisma : ModProjectile
    {
        private static List<int> _ai156_blacklistedTargets = new List<int>();
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("True Terraprisma");
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion
        }
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.manualDirectionChange = true;
            Projectile.damage = 115;
            Projectile.originalDamage = 115;
            // These below are needed for a minion weapon
            Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
            Projectile.minion = true; // Declares this as a minion (has many effects)
            Projectile.DamageType = DamageClass.SummonMeleeSpeed; // Declares the damage type (needed for it to deal damage)
            Projectile.minionSlots = 0f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
            Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override bool? CanCutTiles()
        {
            return false;
        }

        // This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
        public override bool MinionContactDamage()
        {
            return true;
        }
        public override void AI()
        {
            List<int> ai156_blacklistedTargets = _ai156_blacklistedTargets;
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active)
            {
                player.ClearBuff(ModContent.BuffType<TerraFallBuff>());
            }
            if (player.HasBuff(ModContent.BuffType<TerraFallBuff>()))
                {
                    Projectile.timeLeft = 2;
                }
                DelegateMethods.v3_1 = Projectile.AI_156_GetColor().ToVector3();
                Point point2 = Projectile.Center.ToTileCoordinates();
                DelegateMethods.CastLightOpen(point2.X, point2.Y);
            ai156_blacklistedTargets.Clear();
            AI_156_Think(ai156_blacklistedTargets);
            Dust.NewDust(Projectile.Center, Projectile.width / 2, Projectile.height / 2, DustID.TerraBlade);
        }
        private void AI_156_Think(List<int> blacklist)
        {
                int num = 40;
                int num12 = num - 1;
                int num17 = num + 40;
                int num18 = num17 - 1;
                int num19 = num + 1;
            Player player = Main.player[Projectile.owner];
            if (player.active && Vector2.Distance(player.Center, Projectile.Center) > 2000f)
            {
                Projectile.ai[0] = 0f;
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == -1f)
            {
                AI_GetMyGroupIndexAndFillBlackList(blacklist, out var index, out var totalIndexesInGroup);
                AI_156_GetIdlePosition(index, totalIndexesInGroup, out var idleSpot, out var idleRotation);
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Projectile.Center.MoveTowards(idleSpot, 32f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation, 0.2f);
                if (Projectile.Distance(idleSpot) < 2f)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                return;
            }
            if (Projectile.ai[0] == 0f)
            {
                AI_GetMyGroupIndexAndFillBlackList(blacklist, out var index3, out var totalIndexesInGroup3);
                AI_156_GetIdlePosition(index3, totalIndexesInGroup3, out var idleSpot3, out var idleRotation2);
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, idleSpot3, 0.45f);
                Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation2, 0.45f);
                if (Main.rand.NextBool(20))
                {
                    int num21 = AI_156_TryAttackingNPCs(blacklist);
                    if (num21 != -1)
                    {
                        Projectile.ai[0] = Main.rand.NextFromList<int>(num, num17);
                        Projectile.ai[0] = num17;
                        Projectile.ai[1] = num21;
                        Projectile.netUpdate = true;
                    }
                }
                return;
            }
            bool skipBodyCheck = true;
            int num5 = 0;
            int num6 = num12;
            int num7 = 0;
            if (Projectile.ai[0] >= (float)num19)
            {
                num5 = 1;
                num6 = num18;
                num7 = num19;
            }
            int num8 = (int)Projectile.ai[1];
            if (!Main.npc.IndexInRange(num8))
            {
                int num9 = AI_156_TryAttackingNPCs(blacklist, skipBodyCheck);
                if (num9 != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList<int>(num, num17);
                    Projectile.ai[1] = num9;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
                return;
            }
            NPC nPC2 = Main.npc[num8];
            if (!nPC2.CanBeChasedBy(Projectile))
            {
                int num10 = AI_156_TryAttackingNPCs(blacklist, skipBodyCheck);
                if (num10 != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList<int>(num, num17);
                    Projectile.ai[1] = num10;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
                return;
            }
            Projectile.ai[0] -= 1f;
            if (Projectile.ai[0] >= (float)num6)
            {
                Projectile.direction = ((Projectile.Center.X < nPC2.Center.X) ? 1 : (-1));
                if (Projectile.ai[0] == (float)num6)
                {
                    Projectile.localAI[0] = Projectile.Center.X;
                    Projectile.localAI[1] = Projectile.Center.Y;
                }
            }
            float lerpValue2 = Utils.GetLerpValue(num6, num7, Projectile.ai[0], clamped: true);
            if (num5 == 0)
            {
                Vector2 vector4 = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                if (lerpValue2 >= 0.5f)
                {
                    vector4 = Vector2.Lerp(nPC2.Center, Main.player[Projectile.owner].Center, 0.5f);
                }
                Vector2 center2 = nPC2.Center;
                float num11 = (center2 - vector4).ToRotation();
                float num13 = ((Projectile.direction == 1) ? (-(float)Math.PI) : ((float)Math.PI));
                float num14 = num13 + (0f - num13) * lerpValue2 * 2f;
                Vector2 vector5 = num14.ToRotationVector2();
                vector5.Y *= 0.5f;
                vector5.Y *= 0.8f + (float)Math.Sin((float)Projectile.identity * 2.3f) * 0.2f;
                vector5 = vector5.RotatedBy(num11);
                float scaleFactor2 = (center2 - vector4).Length() / 2f;
                Vector2 vector7 = (Projectile.Center = Vector2.Lerp(vector4, center2, 0.5f) + vector5 * scaleFactor2);
                float num15 = MathHelper.WrapAngle(num11 + num14 + 0f);
                Projectile.rotation = num15 + (float)Math.PI / 2f;
                Projectile.velocity = num15.ToRotationVector2() * 10f;
                Projectile.position -= Projectile.velocity;
            }
            if (num5 == 1)
            {
                Vector2 vector2 = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);
                vector2 += new Vector2(0f, Utils.GetLerpValue(0f, 0.4f, lerpValue2, clamped: true) * -100f);
                Vector2 v = nPC2.Center - vector2;
                Vector2 value = v.SafeNormalize(Vector2.Zero) * MathHelper.Clamp(v.Length(), 60f, 150f);
                Vector2 value2 = nPC2.Center + value;
                float lerpValue3 = Utils.GetLerpValue(0.4f, 0.6f, lerpValue2, clamped: true);
                float lerpValue4 = Utils.GetLerpValue(0.6f, 1f, lerpValue2, clamped: true);
                float targetAngle = v.SafeNormalize(Vector2.Zero).ToRotation() + (float)Math.PI / 2f;
                Projectile.rotation = Projectile.rotation.AngleTowards(targetAngle, (float)Math.PI / 5f);
                Projectile.Center = Vector2.Lerp(vector2, nPC2.Center, lerpValue3);
                if (lerpValue4 > 0f)
                {
                    Projectile.Center = Vector2.Lerp(nPC2.Center, value2, lerpValue4);
                }
            }
            if (Projectile.ai[0] == (float)num7)
            {
                int num16 = AI_156_TryAttackingNPCs(blacklist, skipBodyCheck);
                if (num16 != -1)
                {
                    Projectile.ai[0] = Main.rand.NextFromList<int>(num, num17);
                    Projectile.ai[1] = num16;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.ai[0] = -1f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }
            }
        }

        private int AI_156_TryAttackingNPCs(List<int> blackListedTargets, bool skipBodyCheck = false)
        {
            Vector2 center = Main.player[Projectile.owner].Center;
            int result = -1;
            float num = -1f;
            NPC ownerMinionAttackTargetNPC = Projectile.OwnerMinionAttackTargetNPC;
            if (ownerMinionAttackTargetNPC != null && ownerMinionAttackTargetNPC.CanBeChasedBy(Projectile))
            {
                bool flag = true;
                if (!ownerMinionAttackTargetNPC.boss && blackListedTargets.Contains(ownerMinionAttackTargetNPC.whoAmI))
                {
                    flag = false;
                }
                if (ownerMinionAttackTargetNPC.Distance(center) > 1000f)
                {
                    flag = false;
                }
                if (!skipBodyCheck && !Projectile.CanHitWithOwnBody(ownerMinionAttackTargetNPC))
                {
                    flag = false;
                }
                if (flag)
                {
                    return ownerMinionAttackTargetNPC.whoAmI;
                }
            }
            for (int i = 0; i < 200; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC.CanBeChasedBy(Projectile) && (nPC.boss || !blackListedTargets.Contains(i)))
                {
                    float num2 = nPC.Distance(center);
                    if (!(num2 > 1000f) && (!(num2 > num) || num == -1f) && (skipBodyCheck || Projectile.CanHitWithOwnBody(nPC)))
                    {
                        num = num2;
                        result = i;
                    }
                }
            }
            return result;
        }

        private void AI_GetMyGroupIndexAndFillBlackList(List<int> blackListedTargets, out int index, out int totalIndexesInGroup)
        {
            index = 0;
            totalIndexesInGroup = 0;
            for (int i = 0; i < 1000; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == Projectile.type && (projectile.type != 759 || projectile.frame == Main.projFrames[projectile.type] - 1))
                {
                    if (Projectile.whoAmI > i)
                    {
                        index++;
                    }
                    totalIndexesInGroup++;
                }
            }
        }

        private void AI_156_GetIdlePosition(int stackedIndex, int totalIndexes, out Vector2 idleSpot, out float idleRotation)
        {
            Player player = Main.player[Projectile.owner];
            idleRotation = 0f;
            idleSpot = Vector2.Zero;
                int num3 = stackedIndex + 1;
                idleRotation = (float)num3 * ((float)Math.PI * 2f) * (1f / 60f) * (float)player.direction + (float)Math.PI / 2f;
                idleRotation = MathHelper.WrapAngle(idleRotation);
                int num4 = num3 % totalIndexes;
                Vector2 vector = new Vector2(0f, 0.5f).RotatedBy((player.miscCounterNormalized * (2f + (float)num4) + (float)num4 * 0.5f + (float)player.direction * 1.3f) * ((float)Math.PI * 2f)) * 4f;
                idleSpot = idleRotation.ToRotationVector2() * 10f + player.MountedCenter + new Vector2(player.direction * (num3 * -6 - 16), player.gravDir * -15f);
                idleSpot += vector;
                idleRotation += (float)Math.PI / 2f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.ForestGreen;
            return base.PreDraw(ref lightColor);
        }
    }
}