using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Summon;

namespace tsorcRevamp.Projectiles.Summon
{
	public class SamuraiBeetle : ModProjectile
    {
        bool IsPirate = false; //set an ai for it to follow
        bool IsFrog = false;
        bool IsFlinx = true;
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Samurai Beetle");
			Main.projFrames[Projectile.type] = 12; //Flinx: 12, Pirate: 15, Frog: 24
			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

			Main.projPet[Projectile.type] = true;

			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
		}

		public sealed override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.friendly = true;
            Projectile.decidesManualFallThrough = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (IsFlinx)
            {
                Vector2 vector = target.Center.DirectionTo(Projectile.Center);
                vector.X += (-0.5f + Main.rand.NextFloat()) * 13f;
                vector.Y = -5f;
                Projectile.velocity.X = vector.X;
                Projectile.velocity.Y = vector.Y;
                Projectile.netUpdate = true;
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active)
            {
                Projectile.active = false;
                return;
            }
            int num = 800;
            float num12 = 500f;
            float num21 = 300f;

            if (player.dead)
            {
                player.ClearBuff(ModContent.BuffType<SamuraiBeetleBuff>());
            }
            if (player.HasBuff(ModContent.BuffType<SamuraiBeetleBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            Vector2 vector = player.Center;
            if (IsFlinx)
            {
                vector.X -= (45 + player.width / 2) * player.direction;
                vector.X -= Projectile.minionPos * 30 * player.direction;
            }
            else if (IsPirate)
            {
                vector.X -= (15 + player.width / 2) * player.direction;
                vector.X -= Projectile.minionPos * 20 * player.direction;
            }
            else if (IsFrog)
            {
                vector.X -= (35 + player.width / 2) * player.direction;
                vector.X -= Projectile.minionPos * 40 * player.direction;
            }
            bool flag12 = true;
            Projectile.shouldFallThrough = player.position.Y + (float)player.height - 12f > Projectile.position.Y + (float)Projectile.height;
            Projectile.friendly = false;
            int num49 = 0;
            int num50 = 15;
            int attackTarget = -1;
            bool flag13 = true;
            bool flag14 = Projectile.ai[0] == 5f;
            if (IsFlinx)
            {
                flag13 = false;
                Projectile.friendly = true;
            }
            if (IsFrog)
            {
                Projectile.friendly = true;
                num50 = 20;
                num49 = 60;
            }
            bool flag2 = Projectile.ai[0] == 0f;
            if (flag2 && flag12)
            {
                Projectile.Minion_FindTargetInRange(num, ref attackTarget, skipIfCannotHitWithOwnBody: true, CustomEliminationCheck);
            }
            float playerDistance;
            float myDistance;
            bool closerIsMe;
            if (Projectile.ai[0] == 1f)
            {
                Projectile.tileCollide = false;
                float num9 = 0.2f;
                float num10 = 10f;
                int num11 = 200;
                if (num10 < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y))
                {
                    num10 = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                }
                Vector2 vector7 = player.Center - Projectile.Center;
                float num51 = vector7.Length();
                if (num51 > 2000f)
                {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                if (num51 < (float)num11 && player.velocity.Y == 0f && Projectile.position.Y + (float)Projectile.height <= player.position.Y + (float)player.height && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                    if (Projectile.velocity.Y < -6f)
                    {
                        Projectile.velocity.Y = -6f;
                    }
                }
                if (!(num51 < 60f))
                {
                    vector7.Normalize();
                    vector7 *= num10;
                    if (Projectile.velocity.X < vector7.X)
                    {
                        Projectile.velocity.X += num9;
                        if (Projectile.velocity.X < 0f)
                        {
                            Projectile.velocity.X += num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.X > vector7.X)
                    {
                        Projectile.velocity.X -= num9;
                        if (Projectile.velocity.X > 0f)
                        {
                            Projectile.velocity.X -= num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y < vector7.Y)
                    {
                        Projectile.velocity.Y += num9;
                        if (Projectile.velocity.Y < 0f)
                        {
                            Projectile.velocity.Y += num9 * 1.5f;
                        }
                    }
                    if (Projectile.velocity.Y > vector7.Y)
                    {
                        Projectile.velocity.Y -= num9;
                        if (Projectile.velocity.Y > 0f)
                        {
                            Projectile.velocity.Y -= num9 * 1.5f;
                        }
                    }
                }
                if (Projectile.velocity.X != 0f)
                {
                    Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                }
                if (IsFlinx)
                {
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
                    Projectile.rotation = Projectile.rotation.AngleTowards(Projectile.rotation + 0.25f * (float)Projectile.spriteDirection, 0.25f);
                }
                if (IsPirate)
                {
                    Projectile.frameCounter++;
                    if (Projectile.frameCounter > 3)
                    {
                        Projectile.frame++;
                        Projectile.frameCounter = 0;
                    }
                    if ((Projectile.frame < 10) | (Projectile.frame > 13))
                    {
                        Projectile.frame = 10;
                    }
                    Projectile.rotation = Projectile.velocity.X * 0.1f;
                }
                if (IsFrog)
                {
                    int num13 = 3;
                    if (++Projectile.frameCounter >= num13 * 4)
                    {
                        Projectile.frameCounter = 0;
                    }
                    Projectile.frame = 14 + Projectile.frameCounter / num13;
                    Projectile.rotation = Projectile.velocity.X * 0.15f;
                }
            }
            if (Projectile.ai[0] == 2f && Projectile.ai[1] < 0f)
            {
                Projectile.friendly = false;
                Projectile.ai[1] += 1f;
                if (num50 >= 0)
                {
                    Projectile.ai[1] = 0f;
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                    return;
                }
            }
            else if (Projectile.ai[0] == 2f)
            {
                Projectile.spriteDirection = Projectile.direction;
                Projectile.rotation = 0f;
                if (IsPirate)
                {
                    Projectile.friendly = true;
                    Projectile.frame = 4 + (int)((float)num50 - Projectile.ai[1]) / (num50 / 3);
                    if (Projectile.velocity.Y != 0f)
                    {
                        Projectile.frame += 3;
                    }
                }
                if (IsFrog)
                {
                    float num14 = ((float)num50 - Projectile.ai[1]) / (float)num50;
                    if ((double)num14 > 0.25 && (double)num14 < 0.75)
                    {
                        Projectile.friendly = true;
                    }
                    int num15 = (int)(num14 * 5f);
                    if (num15 > 2)
                    {
                        num15 = 4 - num15;
                    }
                    if (Projectile.velocity.Y != 0f)
                    {
                        Projectile.frame = 21 + num15;
                    }
                    else
                    {
                        Projectile.frame = 18 + num15;
                    }
                    if (Projectile.velocity.Y == 0f)
                    {
                        Projectile.velocity.X *= 0.8f;
                    }
                }
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
                Projectile.ai[1] -= 1f;
                if (Projectile.ai[1] <= 0f)
                {
                    if (num49 <= 0)
                    {
                        Projectile.ai[1] = 0f;
                        Projectile.ai[0] = 0f;
                        Projectile.netUpdate = true;
                        return;
                    }
                    Projectile.ai[1] = -num49;
                }
            }
            if (attackTarget >= 0)
            {
                float maxDistance2 = num;
                float num16 = 20f;
                if (IsFrog)
                {
                    num16 = 50f;
                }
                NPC nPC2 = Main.npc[attackTarget];
                Vector2 center = nPC2.Center;
                vector = center;
                if (Projectile.IsInRangeOfMeOrMyOwner(nPC2, maxDistance2, out myDistance, out playerDistance, out closerIsMe))
                {
                    Projectile.shouldFallThrough = nPC2.Center.Y > Projectile.Bottom.Y;
                    bool flag3 = Projectile.velocity.Y == 0f;
                    if (Projectile.wet && Projectile.velocity.Y > 0f && !Projectile.shouldFallThrough)
                    {
                        flag3 = true;
                    }
                    if (center.Y < Projectile.Center.Y - 30f && flag3)
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
                    if (flag13 && Vector2.Distance(Projectile.Center, vector) < num16)
                    {
                        if (Projectile.velocity.Length() > 10f)
                        {
                            Projectile.velocity /= Projectile.velocity.Length() / 10f;
                        }
                        Projectile.ai[0] = 2f;
                        Projectile.ai[1] = num50;
                        Projectile.netUpdate = true;
                        Projectile.direction = ((center.X - Projectile.Center.X > 0f) ? 1 : (-1));
                    }
                }
                if (IsFrog)
                {
                    int num22 = 1;
                    if (center.X - Projectile.Center.X < 0f)
                    {
                        num22 = -1;
                    }
                    vector.X += 20 * -num22;
                }
            }
            if (Projectile.ai[0] == 0f && attackTarget < 0)
            {
                if (Main.player[Projectile.owner].rocketDelay2 > 0)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.netUpdate = true;
                }
                Vector2 vector8 = player.Center - Projectile.Center;
                if (vector8.Length() > 2000f)
                {
                    Projectile.position = player.Center - new Vector2(Projectile.width, Projectile.height) / 2f;
                }
                else if (vector8.Length() > num12 || Math.Abs(vector8.Y) > num21)
                {
                    Projectile.ai[0] = 1f;
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
            if (Projectile.ai[0] == 0f)
            {
                if (attackTarget < 0)
                {
                    if (Projectile.Distance(player.Center) > 60f && Projectile.Distance(vector) > 60f && Math.Sign(vector.X - player.Center.X) != Math.Sign(Projectile.Center.X - player.Center.X))
                    {
                        vector = player.Center;
                    }
                    Rectangle r = Utils.CenteredRectangle(vector, Projectile.Size);
                    for (int i = 0; i < 20; i++)
                    {
                        if (Collision.SolidCollision(r.TopLeft(), r.Width, r.Height))
                        {
                            break;
                        }
                        r.Y += 16;
                        vector.Y += 16f;
                    }
                    Vector2 value = Collision.TileCollision(player.Center - Projectile.Size / 2f, vector - player.Center, Projectile.width, Projectile.height);
                    vector = player.Center - Projectile.Size / 2f + value;
                    if (Projectile.Distance(vector) < 32f)
                    {
                        float num23 = player.Center.Distance(vector);
                        if (player.Center.Distance(Projectile.Center) < num23)
                        {
                            vector = Projectile.Center;
                        }
                    }
                    Vector2 vector9 = player.Center - vector;
                    if (vector9.Length() > num12 || Math.Abs(vector9.Y) > num21)
                    {
                        Rectangle r2 = Utils.CenteredRectangle(player.Center, Projectile.Size);
                        Vector2 value2 = vector - player.Center;
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
                        vector = value3 + Projectile.Size / 2f;
                    }
                }
                Projectile.tileCollide = true;
                float num25 = 0.5f;
                float num26 = 4f;
                float num27 = 4f;
                float num28 = 0.1f;
                if (IsFlinx && attackTarget != -1)
                {
                    num25 = 0.65f;
                    num26 = 5.5f;
                    num27 = 5.5f;
                }
                if (IsPirate && attackTarget != -1)
                {
                    num25 = 1f;
                    num26 = 8f;
                    num27 = 8f;
                }
                if (IsFrog && attackTarget != -1)
                {
                    num25 = 0.7f;
                    num26 = 6f;
                    num27 = 6f;
                }
                if (num27 < Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y))
                {
                    num27 = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
                    num25 = 0.7f;
                }
                int num29 = 0;
                bool flag4 = false;
                float num30 = vector.X - Projectile.Center.X;
                Vector2 vector2 = vector - Projectile.Center;
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
                    bool flag5 = true;
                    if (IsPirate)
                    {
                        flag5 = false;
                    }
                    if (IsFrog && attackTarget == -1)
                    {
                        flag5 = false;
                    }
                    if (IsFlinx)
                    {
                        flag5 = attackTarget > -1 && Main.npc[attackTarget].Hitbox.Intersects(Projectile.Hitbox);
                    }
                    if (flag5)
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
                bool flag6 = Math.Abs(vector2.X) >= 64f || (vector2.Y <= -48f && Math.Abs(vector2.X) >= 8f);
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
                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);
                float num34 = Utils.GetLerpValue(0f, 100f, vector2.Y, clamped: true) * Utils.GetLerpValue(-2f, -6f, Projectile.velocity.Y, clamped: true);
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
                        int num36 = (int)(Projectile.position.Y + (float)Projectile.height) / 16;
                        if (!WorldGen.SolidTile(num35, num36) && !Main.tile[num35, num36].IsHalfBlock && Main.tile[num35, num36].Slope <= 0 && (!TileID.Sets.Platforms[Main.tile[num35, num36].TileType] || !Main.tile[num35, num36].HasTile || Main.tile[num35, num36].IsActuated))
                        {
                            continue;
                        }
                        try
                        {
                            num35 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                            num36 = (int)(Projectile.position.Y + (float)(Projectile.height / 2)) / 16;
                            num35 += num29;
                            num35 += (int)Projectile.velocity.X;
                            if (!WorldGen.SolidTile(num35, num36 - 1) && !WorldGen.SolidTile(num35, num36 - 2))
                            {
                                Projectile.velocity.Y = -5.1f;
                            }
                            else if (!WorldGen.SolidTile(num35, num36 - 2))
                            {
                                Projectile.velocity.Y = -7.1f;
                            }
                            else if (WorldGen.SolidTile(num35, num36 - 5))
                            {
                                Projectile.velocity.Y = -11.1f;
                            }
                            else if (WorldGen.SolidTile(num35, num36 - 4))
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
                    if (vector.Y - Projectile.Center.Y < -48f)
                    {
                        float num37 = vector.Y - Projectile.Center.Y;
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
                if (IsFlinx)
                {
                    if (Projectile.velocity.Y == 0f)
                    {
                        Projectile.rotation = Projectile.rotation.AngleTowards(0f, 0.3f);
                        if (Projectile.velocity.X == 0f)
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                        else if (Math.Abs(Projectile.velocity.X) >= 0.5f)
                        {
                            Projectile.frameCounter += (int)Math.Abs(Projectile.velocity.X);
                            Projectile.frameCounter++;
                            if (Projectile.frameCounter > 10)
                            {
                                Projectile.frame++;
                                Projectile.frameCounter = 0;
                            }
                            if (Projectile.frame < 2 || Projectile.frame >= Main.projFrames[Projectile.type])
                            {
                                Projectile.frame = 2;
                            }
                        }
                        else
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                    }
                    else if (Projectile.velocity.Y != 0f)
                    {
                        Projectile.rotation = Math.Min(4f, Projectile.velocity.Y) * -0.1f;
                        if (Projectile.spriteDirection == -1)
                        {
                            Projectile.rotation -= (float)Math.PI * 2f;
                        }
                        Projectile.frameCounter = 0;
                        Projectile.frame = 1;
                    }
                }
                if (IsPirate)
                {
                    Projectile.rotation = 0f;
                    if (Projectile.velocity.Y == 0f)
                    {
                        if (Projectile.velocity.X == 0f)
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                        else if (Math.Abs(Projectile.velocity.X) >= 0.5f)
                        {
                            Projectile.frameCounter += (int)Math.Abs(Projectile.velocity.X);
                            Projectile.frameCounter++;
                            if (Projectile.frameCounter > 10)
                            {
                                Projectile.frame++;
                                Projectile.frameCounter = 0;
                            }
                            if (Projectile.frame >= 4)
                            {
                                Projectile.frame = 0;
                            }
                        }
                        else
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                    }
                    else if (Projectile.velocity.Y != 0f)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame = 14;
                    }
                }
                if (IsFrog)
                {
                    Projectile.rotation = 0f;
                    if (Projectile.velocity.Y == 0f)
                    {
                        if (Projectile.velocity.X == 0f)
                        {
                            int num38 = 4;
                            if (++Projectile.frameCounter >= 7 * num38 && Main.rand.NextBool(50))
                            {
                                Projectile.frameCounter = 0;
                            }
                            int num39 = Projectile.frameCounter / num38;
                            if (num39 >= 4)
                            {
                                num39 = 6 - num39;
                            }
                            if (num39 < 0)
                            {
                                num39 = 0;
                            }
                            Projectile.frame = 1 + num39;
                        }
                        else if (Math.Abs(Projectile.velocity.X) >= 0.5f)
                        {
                            Projectile.frameCounter += (int)Math.Abs(Projectile.velocity.X);
                            Projectile.frameCounter++;
                            int num40 = 15;
                            int num41 = 8;
                            if (Projectile.frameCounter >= num41 * num40)
                            {
                                Projectile.frameCounter = 0;
                            }
                            int num42 = Projectile.frameCounter / num40;
                            Projectile.frame = num42 + 5;
                        }
                        else
                        {
                            Projectile.frame = 0;
                            Projectile.frameCounter = 0;
                        }
                    }
                    else if (Projectile.velocity.Y != 0f)
                    {
                        if (Projectile.velocity.Y < 0f)
                        {
                            if (Projectile.frame > 9 || Projectile.frame < 5)
                            {
                                Projectile.frame = 5;
                                Projectile.frameCounter = 0;
                            }
                            if (++Projectile.frameCounter >= 1 && Projectile.frame < 9)
                            {
                                Projectile.frame++;
                                Projectile.frameCounter = 0;
                            }
                        }
                        else
                        {
                            if (Projectile.frame > 13 || Projectile.frame < 9)
                            {
                                Projectile.frame = 9;
                                Projectile.frameCounter = 0;
                            }
                            if (++Projectile.frameCounter >= 2 && Projectile.frame < 11)
                            {
                                Projectile.frame++;
                                Projectile.frameCounter = 0;
                            }
                        }
                    }
                }
                Projectile.velocity.Y += 0.4f + num34 * 1f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
            }
            if (!IsPirate)
            {
                return;
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.velocity.X == 0f)
            {
                Projectile.localAI[0] += 1f;
            }/*
            if (Projectile.localAI[0] >= (float)Main.rand.Next(900, 1200)) //poop code
            {
                Projectile.localAI[0] = 0f;
                for (int l = 0; l < 6; l++)
                {
                    int num45 = Dust.NewDust(Projectile.Center + Vector2.UnitX * -Projectile.direction * 8f - Vector2.One * 5f + Vector2.UnitY * 8f, 3, 6, 216, -Projectile.direction, 1f);
                    Main.dust[num45].velocity /= 2f;
                    Main.dust[num45].scale = 0.8f;
                }
                int num46 = Gore.NewGore(Projectile.GetSource_FromThis(),Projectile.Center + Vector2.UnitX * -Projectile.direction * 8f, Vector2.Zero, Main.rand.Next(580, 583));
                Main.gore[num46].velocity /= 2f;
                Main.gore[num46].velocity.Y = Math.Abs(Main.gore[num46].velocity.Y);
                Main.gore[num46].velocity.X = (0f - Math.Abs(Main.gore[num46].velocity.X)) * (float)Projectile.direction;
            }*/
        }
        private bool CustomEliminationCheck(Entity otherEntity, int currentTarget)
        {
            return true;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (IsFrog && Projectile.ai[0] == 2f && Utils.CenteredRectangle(Projectile.Center + new Vector2(Projectile.spriteDirection * 30, 0f), new Vector2(50f, 20f)).Intersects(targetHitbox))
            {
                return true;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }

        public override bool? CanCutTiles()
		{
			return false;
		}
		public override bool MinionContactDamage()
		{
			return true;
		}
    }
}