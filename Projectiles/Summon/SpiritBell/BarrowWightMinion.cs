using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Summon.SpiritBell
{
    class BarrowWightMinion : ModProjectile
    {

        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        float chargetimer0;
        float chargeTimer;
        float chargeTimer2;
        float chargeTimer3;
        NPC target;

        public override void SetStaticDefaults()
        {

            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            Main.projPet[Projectile.type] = true;

            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 104;
            Projectile.height = 93;
            Projectile.tileCollide = false; // Makes the minion go through tiles freely
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.aiStyle = -1;
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            chargeTimer2 = 0f;
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 1200);
                target.AddBuff(BuffID.Chilled, 1200);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000);
            }
            if (hit.Crit == true)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.3f, 0.3f, 200, default, 1f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 3f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(Projectile.position, Projectile.height, Projectile.width, 45, 0.2f, 0.2f, 200, default, 4f);
            }
        }
        public override bool? CanCutTiles()
        {
            return false;
        }

        public override bool MinionContactDamage()
        {
            return true;
        }
        public override void AI()
        {
            Idle();
        }

        public void Attack()
        {
        }
        public void Idle()
        {
            #region Full AI 22 Code
            /*
            else if (this.aiStyle == 22)
                {
                    bool flag20 = false;
                    bool flag21 = this.type == 330 && !Main.pumpkinMoon;
                    if (this.type == 253 && !Main.eclipse)
                    {
                        flag21 = true;
                    }
                    if (this.type == 490 && Main.dayTime)
                    {
                        flag21 = true;
                    }
                    if (this.justHit)
                    {
                        this.ai[2] = 0f;
                    }
                    if (this.type == 316 && (Main.player[this.target].dead || Vector2.Distance(base.Center, Main.player[this.target].Center) > 3000f))
                    {
                        this.TargetClosest();
                        if (Main.player[this.target].dead || Vector2.Distance(base.Center, Main.player[this.target].Center) > 3000f)
                        {
                            this.EncourageDespawn(10);
                            flag20 = true;
                            flag21 = true;
                        }
                    }
                    if (flag21)
                    {
                        if (base.velocity.X == 0f)
                        {
                            base.velocity.X = (float)Main.rand.Next(-1, 2) * 1.5f;
                            this.netUpdate = true;
                        }
                    }
                    else if (this.ai[2] >= 0f)
                    {
                        int num827 = 16;
                        bool flag22 = false;
                        bool flag23 = false;
                        if (base.position.X > this.ai[0] - (float)num827 && base.position.X < this.ai[0] + (float)num827)
                        {
                            flag22 = true;
                        }
                        else if ((base.velocity.X < 0f && base.direction > 0) || (base.velocity.X > 0f && base.direction < 0))
                        {
                            flag22 = true;
                        }
                        num827 += 24;
                        if (base.position.Y > this.ai[1] - (float)num827 && base.position.Y < this.ai[1] + (float)num827)
                        {
                            flag23 = true;
                        }
                        if (flag22 && flag23)
                        {
                            this.ai[2] += 1f;
                            if (this.ai[2] >= 30f && num827 == 16)
                            {
                                flag20 = true;
                            }
                            if (this.ai[2] >= 60f)
                            {
                                this.ai[2] = -200f;
                                base.direction *= -1;
                                base.velocity.X *= -1f;
                                this.collideX = false;
                            }
                        }
                        else
                        {
                            this.ai[0] = base.position.X;
                            this.ai[1] = base.position.Y;
                            this.ai[2] = 0f;
                        }
                        this.TargetClosest();
                    }
                    else if (this.type == 253)
                    {
                        this.TargetClosest();
                        this.ai[2] += 2f;
                    }
                    else
                    {
                        if (this.type == 330)
                        {
                            this.ai[2] += 0.1f;
                        }
                        else
                        {
                            this.ai[2] += 1f;
                        }
                        if (Main.player[this.target].position.X + (float)(Main.player[this.target].width / 2) > base.position.X + (float)(base.width / 2))
                        {
                            base.direction = -1;
                        }
                        else
                        {
                            base.direction = 1;
                        }
                    }
                    int num828 = (int)((base.position.X + (float)(base.width / 2)) / 16f) + base.direction * 2;
                    int num831 = (int)((base.position.Y + (float)base.height) / 16f);
                    bool flag25 = true;
                    bool flag26 = false;
                    int num832 = 3;
                    if (this.type == 122)
                    {
                        if (this.justHit)
                        {
                            this.ai[3] = 0f;
                            this.localAI[1] = 0f;
                        }
                        if (Main.netMode != 1 && this.ai[3] == 32f && !Main.player[this.target].npcTypeNoAggro[this.type])
                        {
                            float num833 = 7f;
                            Vector2 vector245 = default(Vector2);
                            ((Vector2)(ref vector245))._002Ector(base.position.X + (float)base.width * 0.5f, base.position.Y + (float)base.height * 0.5f);
                            float num834 = Main.player[this.target].position.X + (float)(Main.player[this.target].width / 2) - vector245.X;
                            float num835 = Main.player[this.target].position.Y + (float)(Main.player[this.target].height / 2) - vector245.Y;
                            float num836 = (float)Math.Sqrt(num834 * num834 + num835 * num835);
                            float num837 = num836;
                            num836 = num833 / num836;
                            num834 *= num836;
                            num835 *= num836;
                            float num838 = 0.0125f;
                            Vector2 vector246 = Utils.RotatedByRandom(new Vector2(num834, num835), num838 * ((float)Math.PI * 2f));
                            num834 = vector246.X;
                            num835 = vector246.Y;
                            int num839 = 25;
                            int num840 = 84;
                            int num842 = Projectile.NewProjectile(this.GetSpawnSource_ForProjectile(), vector245.X, vector245.Y, num834, num835, num840, num839, 0f, Main.myPlayer);
                        }
                        num832 = 8;
                        if (this.ai[3] > 0f)
                        {
                            this.ai[3] += 1f;
                            if (this.ai[3] >= 64f)
                            {
                                this.ai[3] = 0f;
                            }
                        }
                        if (Main.netMode != 1 && this.ai[3] == 0f)
                        {
                            this.localAI[1] += 1f;
                            if (this.localAI[1] > 120f && Collision.CanHit(base.position, base.width, base.height, Main.player[this.target].position, Main.player[this.target].width, Main.player[this.target].height) && !Main.player[this.target].npcTypeNoAggro[this.type])
                            {
                                this.localAI[1] = 0f;
                                this.ai[3] = 1f;
                                this.netUpdate = true;
                            }
                        }
                    }
                    else if (this.type == 75)
                    {
                        num832 = 4;
                        base.position += this.netOffset;
                        if (Main.rand.Next(6) == 0)
                        {
                            int num843 = Dust.NewDust(base.position, base.width, base.height, 55, 0f, 0f, 200, this.color);
                            Dust dust53 = Main.dust[num843];
                            Dust dust87 = dust53;
                            dust87.velocity *= 0.3f;
                        }
                        if (Main.rand.Next(40) == 0)
                        {
                            SoundEngine.PlaySound(27, (int)base.position.X, (int)base.position.Y);
                        }
                        base.position -= this.netOffset;
                    }
                    else if (this.type == 169)
                    {
                        base.position += this.netOffset;
                        Lighting.AddLight((int)((base.position.X + (float)(base.width / 2)) / 16f), (int)((base.position.Y + (float)(base.height / 2)) / 16f), 0f, 0.6f, 0.75f);
                        this.alpha = 30;
                        if (Main.rand.Next(3) == 0)
                        {
                            Vector2 val40 = base.position;
                            int num1672 = base.width;
                            int num1673 = base.height;
                            newColor = default(Color);
                            int num844 = Dust.NewDust(val40, num1672, num1673, 92, 0f, 0f, 200, newColor);
                            Dust dust54 = Main.dust[num844];
                            Dust dust87 = dust54;
                            dust87.velocity *= 0.3f;
                            Main.dust[num844].noGravity = true;
                        }
                        base.position -= this.netOffset;
                        if (this.justHit)
                        {
                            this.ai[3] = 0f;
                            this.localAI[1] = 0f;
                        }
                        float num845 = 5f;
                        Vector2 vector247 = default(Vector2);
                        ((Vector2)(ref vector247))._002Ector(base.position.X + (float)base.width * 0.5f, base.position.Y + (float)base.height * 0.5f);
                        float num846 = Main.player[this.target].position.X + (float)(Main.player[this.target].width / 2) - vector247.X;
                        float num847 = Main.player[this.target].position.Y + (float)(Main.player[this.target].height / 2) - vector247.Y;
                        float num848 = (float)Math.Sqrt(num846 * num846 + num847 * num847);
                        float num849 = num848;
                        num848 = num845 / num848;
                        num846 *= num848;
                        num847 *= num848;
                        if (num846 > 0f)
                        {
                            base.direction = 1;
                        }
                        else
                        {
                            base.direction = -1;
                        }
                        this.spriteDirection = base.direction;
                        if (base.direction < 0)
                        {
                            this.rotation = (float)Math.Atan2(0f - num847, 0f - num846);
                        }
                        else
                        {
                            this.rotation = (float)Math.Atan2(num847, num846);
                        }
                        if (Main.netMode != 1 && this.ai[3] == 16f)
                        {
                            int num850 = 45;
                            int num851 = 128;
                            int num853 = Projectile.NewProjectile(this.GetSpawnSource_ForProjectile(), vector247.X, vector247.Y, num846, num847, num851, num850, 0f, Main.myPlayer);
                        }
                        num832 = 10;
                        if (this.ai[3] > 0f)
                        {
                            this.ai[3] += 1f;
                            if (this.ai[3] >= 64f)
                            {
                                this.ai[3] = 0f;
                            }
                        }
                        if (Main.netMode != 1 && this.ai[3] == 0f)
                        {
                            this.localAI[1] += 1f;
                            if (this.localAI[1] > 120f && Collision.CanHit(base.position, base.width, base.height, Main.player[this.target].position, Main.player[this.target].width, Main.player[this.target].height))
                            {
                                this.localAI[1] = 0f;
                                this.ai[3] = 1f;
                                this.netUpdate = true;
                            }
                        }
                    }
                    else if (this.type == 268)
                    {
                        this.rotation = base.velocity.X * 0.1f;
                        num832 = ((!(Main.player[this.target].Center.Y < base.Center.Y)) ? 6 : 12);
                        if (Main.netMode != 1 && !this.confused)
                        {
                            this.ai[3] += 1f;
                            if (this.justHit)
                            {
                                this.ai[3] = -45f;
                                this.localAI[1] = 0f;
                            }
                            if (Main.netMode != 1 && this.ai[3] >= (float)(60 + Main.rand.Next(60)))
                            {
                                this.ai[3] = 0f;
                                if (Collision.CanHit(base.position, base.width, base.height, Main.player[this.target].position, Main.player[this.target].width, Main.player[this.target].height))
                                {
                                    float num854 = 10f;
                                    Vector2 vector248 = default(Vector2);
                                    ((Vector2)(ref vector248))._002Ector(base.position.X + (float)base.width * 0.5f - 4f, base.position.Y + (float)base.height * 0.7f);
                                    float num855 = Main.player[this.target].position.X + (float)(Main.player[this.target].width / 2) - vector248.X;
                                    float num856 = Math.Abs(num855) * 0.1f;
                                    float num857 = Main.player[this.target].position.Y + (float)(Main.player[this.target].height / 2) - vector248.Y - num856;
                                    num855 += (float)Main.rand.Next(-10, 11);
                                    num857 += (float)Main.rand.Next(-30, 21);
                                    float num858 = (float)Math.Sqrt(num855 * num855 + num857 * num857);
                                    float num859 = num858;
                                    num858 = num854 / num858;
                                    num855 *= num858;
                                    num857 *= num858;
                                    int num860 = 40;
                                    int num861 = 288;
                                    int num862 = Projectile.NewProjectile(this.GetSpawnSource_ForProjectile(), vector248.X, vector248.Y, num855, num857, num861, num860, 0f, Main.myPlayer);
                                }
                            }
                        }
                    }
                    if (this.type == 490)
                    {
                        num832 = 4;
                        if (this.target >= 0)
                        {
                            val29 = Main.player[this.target].Center - base.Center;
                            float num864 = ((Vector2)(ref val29)).Length();
                            num864 /= 70f;
                            if (num864 > 8f)
                            {
                                num864 = 8f;
                            }
                            num832 += (int)num864;
                        }
                    }
                    if (base.position.Y + (float)base.height > Main.player[this.target].position.Y)
                    {
                        if (this.type == 330)
                        {
                            flag25 = false;
                        }
                        else
                        {
                            for (int num865 = num831; num865 < num831 + num832; num865++)
                            {
                                if (Main.tile[num828, num865] == null)
                                {
                                    Main.tile[num828, num865] = default(Tile);
                                }
                                if ((Main.tile[num828, num865].nactive() && Main.tileSolid[Main.tile[num828, num865].type]) || Main.tile[num828, num865].liquid > 0)
                                {
                                    if (num865 <= num831 + 1)
                                    {
                                        flag26 = true;
                                    }
                                    flag25 = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (Main.player[this.target].npcTypeNoAggro[this.type])
                    {
                        bool flag27 = false;
                        for (int num866 = num831; num866 < num831 + num832 - 2; num866++)
                        {
                            if (Main.tile[num828, num866] == null)
                            {
                                Main.tile[num828, num866] = default(Tile);
                            }
                            if ((Main.tile[num828, num866].nactive() && Main.tileSolid[Main.tile[num828, num866].type]) || Main.tile[num828, num866].liquid > 0)
                            {
                                flag27 = true;
                                break;
                            }
                        }
                        this.directionY = (!flag27).ToDirectionInt();
                    }
                    if (this.type == 169 || this.type == 268)
                    {
                        for (int num867 = num831 - 3; num867 < num831; num867++)
                        {
                            if (Main.tile[num828, num867] == null)
                            {
                                Main.tile[num828, num867] = default(Tile);
                            }
                            if ((Main.tile[num828, num867].nactive() && Main.tileSolid[Main.tile[num828, num867].type] && !TileID.Sets.Platforms[Main.tile[num828, num867].type]) || Main.tile[num828, num867].liquid > 0)
                            {
                                flag26 = false;
                                flag20 = true;
                                break;
                            }
                        }
                    }
                    if (flag20)
                    {
                        flag26 = false;
                        flag25 = true;
                        if (this.type == 268)
                        {
                            base.velocity.Y += 2f;
                        }
                    }
                    if (flag25)
                    {
                        if (this.type == 75 || this.type == 169)
                        {
                            base.velocity.Y += 0.2f;
                            if (base.velocity.Y > 2f)
                            {
                                base.velocity.Y = 2f;
                            }
                        }
                        else if (this.type == 490)
                        {
                            base.velocity.Y += 0.03f;
                            if ((double)base.velocity.Y > 0.75)
                            {
                                base.velocity.Y = 0.75f;
                            }
                        }
                        else
                        {
                            base.velocity.Y += 0.1f;
                            if (this.type == 316 && flag21)
                            {
                                base.velocity.Y -= 0.05f;
                                if (base.velocity.Y > 6f)
                                {
                                    base.velocity.Y = 6f;
                                }
                            }
                            else if (base.velocity.Y > 3f)
                            {
                                base.velocity.Y = 3f;
                            }
                        }
                    }
                    else
                    {
                        if (this.type == 75 || this.type == 169)
                        {
                            if ((this.directionY < 0 && base.velocity.Y > 0f) || flag26)
                            {
                                base.velocity.Y -= 0.2f;
                            }
                        }
                        else if (this.type == 490)
                        {
                            if ((this.directionY < 0 && base.velocity.Y > 0f) || flag26)
                            {
                                base.velocity.Y -= 0.075f;
                            }
                            if (base.velocity.Y < -0.75f)
                            {
                                base.velocity.Y = -0.75f;
                            }
                        }
                        else if (this.directionY < 0 && base.velocity.Y > 0f)
                        {
                            base.velocity.Y -= 0.1f;
                        }
                        if (base.velocity.Y < -4f)
                        {
                            base.velocity.Y = -4f;
                        }
                    }
                    if (this.type == 75 && base.wet)
                    {
                        base.velocity.Y -= 0.2f;
                        if (base.velocity.Y < -2f)
                        {
                            base.velocity.Y = -2f;
                        }
                    }
                    if (this.collideX)
                    {
                        base.velocity.X = base.oldVelocity.X * -0.4f;
                        if (base.direction == -1 && base.velocity.X > 0f && base.velocity.X < 1f)
                        {
                            base.velocity.X = 1f;
                        }
                        if (base.direction == 1 && base.velocity.X < 0f && base.velocity.X > -1f)
                        {
                            base.velocity.X = -1f;
                        }
                    }
                    if (this.collideY)
                    {
                        base.velocity.Y = base.oldVelocity.Y * -0.25f;
                        if (base.velocity.Y > 0f && base.velocity.Y < 1f)
                        {
                            base.velocity.Y = 1f;
                        }
                        if (base.velocity.Y < 0f && base.velocity.Y > -1f)
                        {
                            base.velocity.Y = -1f;
                        }
                    }
                    float num868 = 2f;
                    if (this.type == 75)
                    {
                        num868 = 3f;
                    }
                    if (this.type == 253)
                    {
                        num868 = 4f;
                    }
                    if (this.type == 490)
                    {
                        num868 = 1.5f;
                    }
                    if (this.type == 330)
                    {
                        this.alpha = 0;
                        num868 = 4f;
                        if (!flag21)
                        {
                            this.TargetClosest();
                        }
                        else
                        {
                            this.EncourageDespawn(10);
                        }
                        if (base.direction < 0 && base.velocity.X > 0f)
                        {
                            base.velocity.X *= 0.9f;
                        }
                        if (base.direction > 0 && base.velocity.X < 0f)
                        {
                            base.velocity.X *= 0.9f;
                        }
                    }
                    if (base.direction == -1 && base.velocity.X > 0f - num868)
                    {
                        base.velocity.X -= 0.1f;
                        if (base.velocity.X > num868)
                        {
                            base.velocity.X -= 0.1f;
                        }
                        else if (base.velocity.X > 0f)
                        {
                            base.velocity.X += 0.05f;
                        }
                        if (base.velocity.X < 0f - num868)
                        {
                            base.velocity.X = 0f - num868;
                        }
                    }
                    else if (base.direction == 1 && base.velocity.X < num868)
                    {
                        base.velocity.X += 0.1f;
                        if (base.velocity.X < 0f - num868)
                        {
                            base.velocity.X += 0.1f;
                        }
                        else if (base.velocity.X < 0f)
                        {
                            base.velocity.X -= 0.05f;
                        }
                        if (base.velocity.X > num868)
                        {
                            base.velocity.X = num868;
                        }
                    }
                    num868 = ((this.type != 490) ? 1.5f : 1f);
                    if (this.directionY == -1 && base.velocity.Y > 0f - num868)
                    {
                        base.velocity.Y -= 0.04f;
                        if (base.velocity.Y > num868)
                        {
                            base.velocity.Y -= 0.05f;
                        }
                        else if (base.velocity.Y > 0f)
                        {
                            base.velocity.Y += 0.03f;
                        }
                        if (base.velocity.Y < 0f - num868)
                        {
                            base.velocity.Y = 0f - num868;
                        }
                    }
                    else if (this.directionY == 1 && base.velocity.Y < num868)
                    {
                        base.velocity.Y += 0.04f;
                        if (base.velocity.Y < 0f - num868)
                        {
                            base.velocity.Y += 0.05f;
                        }
                        else if (base.velocity.Y < 0f)
                        {
                            base.velocity.Y -= 0.03f;
                        }
                        if (base.velocity.Y > num868)
                        {
                            base.velocity.Y = num868;
                        }
                    }
                    if (this.type == 122)
                    {
                        Lighting.AddLight((int)base.position.X / 16, (int)base.position.Y / 16, 0.4f, 0f, 0.25f);
                    }
                }*/

            #endregion


            Player owner = Main.player[Projectile.owner];
            bool yes = true;
            bool flag20 = false;
            bool flag21 = yes && !Main.pumpkinMoon;

            //if (Projectile.justHit)
            {
                Projectile.ai[2] = 0f;
            }
            if (flag21)
            {
                if (Projectile.velocity.X == 0f)
                {
                    Projectile.velocity.X = (float)Main.rand.Next(-1, 2) * 1.5f;
                    Projectile.netUpdate = true;
                }
            }
            else if (Projectile.ai[2] >= 0f)
            {
                int num827 = 16;
                bool flag22 = false;
                bool flag23 = false;
                if (Projectile.position.X > Projectile.ai[0] - (float)num827 && Projectile.position.X < Projectile.ai[0] + (float)num827)
                {
                    flag22 = true;
                }
                else if ((Projectile.velocity.X < 0f && Projectile.direction > 0) || (Projectile.velocity.X > 0f && Projectile.direction < 0))
                {
                    flag22 = true;
                }
                num827 += 24;
                if (Projectile.position.Y > Projectile.ai[1] - (float)num827 && Projectile.position.Y < Projectile.ai[1] + (float)num827)
                {
                    flag23 = true;
                }
                if (flag22 && flag23)
                {
                    Projectile.ai[2] += 1f;
                    if (Projectile.ai[2] >= 30f && num827 == 16)
                    {
                        flag20 = true;
                    }
                    if (Projectile.ai[2] >= 60f)
                    {
                        Projectile.ai[2] = -200f;
                        Projectile.direction *= -1;
                        Projectile.velocity.X *= -1f;
                        //Projectile.collideX = false;
                    }
                }
                else
                {
                    Projectile.ai[0] = Projectile.position.X;
                    Projectile.ai[1] = Projectile.position.Y;
                    Projectile.ai[2] = 0f;
                }
                //Projectile.TargetClosest();
            }
            else
            {
                if (yes)
                {
                    Projectile.ai[2] += 0.1f;
                }
                else
                {
                    Projectile.ai[2] += 1f;
                }
                if (owner.position.X + (float)(owner.width / 2) > Projectile.position.X + (float)(Projectile.width / 2))
                {
                    Projectile.direction = -1;
                }
                else
                {
                    Projectile.direction = 1;
                }
            }
            int num828 = (int)((Projectile.position.X + (float)(Projectile.width / 2)) / 16f) + Projectile.direction * 2;
            int num831 = (int)((Projectile.position.Y + (float)Projectile.height) / 16f);
            bool flag25 = true;
            bool flag26 = false;
            int num832 = 3;
            if (Projectile.position.Y + (float)Projectile.height > owner.position.Y)
            {
                if (yes)
                {
                    flag25 = false;
                }
                else
                {
                    for (int num865 = num831; num865 < num831 + num832; num865++)
                    {
                        if (Main.tile[num828, num865] == null)
                        {
                            //Main.tile[num828, num865] = default(Tile);
                        }
                        if ((Main.tile[num828, num865].IsActuated && Main.tileSolid[Main.tile[num828, num865].TileType]) || Main.tile[num828, num865].LiquidType > 0)
                        {
                            if (num865 <= num831 + 1)
                            {
                                flag26 = true;
                            }
                            flag25 = false;
                            break;
                        }
                    }
                }
            }
            if (owner.npcTypeNoAggro[Projectile.type])
            {
                bool flag27 = false;
                for (int num866 = num831; num866 < num831 + num832 - 2; num866++)
                {
                    if (Main.tile[num828, num866] == null)
                    {
                        //Main.tile[num828, num866] = default(Tile);
                    }
                    if ((Main.tile[num828, num866].IsActuated && Main.tileSolid[Main.tile[num828, num866].TileType]) || Main.tile[num828, num866].LiquidType > 0)
                    {
                        flag27 = true;
                        break;
                    }
                }
                //Projectile.directionY = (!flag27).ToDirectionInt();
            }
            if (flag20)
            {
                flag26 = false;
                flag25 = true;
            }
            if (flag25)
            {
                Projectile.velocity.Y += 0.1f;
                if (Projectile.velocity.Y > 3f)
                {
                    Projectile.velocity.Y = 3f;
                }
            }
            else
            {
                if (/*Projectile.directionY < 0 &&*/ Projectile.velocity.Y > 0f)
                {
                    Projectile.velocity.Y -= 0.1f;
                }
                if (Projectile.velocity.Y < -4f)
                {
                    Projectile.velocity.Y = -4f;
                }
            }
            //if (Projectile.collideX)
            {
                Projectile.velocity.X = Projectile.oldVelocity.X * -0.4f;
                if (Projectile.direction == -1 && Projectile.velocity.X > 0f && Projectile.velocity.X < 1f)
                {
                    Projectile.velocity.X = 1f;
                }
                if (Projectile.direction == 1 && Projectile.velocity.X < 0f && Projectile.velocity.X > -1f)
                {
                    Projectile.velocity.X = -1f;
                }
            }
            //if (Projectile.collideY)
            {
                Projectile.velocity.Y = Projectile.oldVelocity.Y * -0.25f;
                if (Projectile.velocity.Y > 0f && Projectile.velocity.Y < 1f)
                {
                    Projectile.velocity.Y = 1f;
                }
                if (Projectile.velocity.Y < 0f && Projectile.velocity.Y > -1f)
                {
                    Projectile.velocity.Y = -1f;
                }
            }
            float num868 = 2f;
            if (yes)
            {
                Projectile.alpha = 0;
                num868 = 4f;
                if (!flag21)
                {
                    //Projectile.TargetClosest();
                }
                else
                {
                    //Projectile.EncourageDespawn(10);
                }
                if (Projectile.direction < 0 && Projectile.velocity.X > 0f)
                {
                    Projectile.velocity.X *= 0.9f;
                }
                if (Projectile.direction > 0 && Projectile.velocity.X < 0f)
                {
                    Projectile.velocity.X *= 0.9f;
                }
            }
            if (Projectile.direction == -1 && Projectile.velocity.X > 0f - num868)
            {
                Projectile.velocity.X -= 0.1f;
                if (Projectile.velocity.X > num868)
                {
                    Projectile.velocity.X -= 0.1f;
                }
                else if (Projectile.velocity.X > 0f)
                {
                    Projectile.velocity.X += 0.05f;
                }
                if (Projectile.velocity.X < 0f - num868)
                {
                    Projectile.velocity.X = 0f - num868;
                }
            }
            else if (Projectile.direction == 1 && Projectile.velocity.X < num868)
            {
                Projectile.velocity.X += 0.1f;
                if (Projectile.velocity.X < 0f - num868)
                {
                    Projectile.velocity.X += 0.1f;
                }
                else if (Projectile.velocity.X < 0f)
                {
                    Projectile.velocity.X -= 0.05f;
                }
                if (Projectile.velocity.X > num868)
                {
                    Projectile.velocity.X = num868;
                }
            }
            num868 = ((Projectile.type != 490) ? 1.5f : 1f);
            if (/*Projectile.directionY == -1 &&*/ Projectile.velocity.Y > 0f - num868)
            {
                Projectile.velocity.Y -= 0.04f;
                if (Projectile.velocity.Y > num868)
                {
                    Projectile.velocity.Y -= 0.05f;
                }
                else if (Projectile.velocity.Y > 0f)
                {
                    Projectile.velocity.Y += 0.03f;
                }
                if (Projectile.velocity.Y < 0f - num868)
                {
                    Projectile.velocity.Y = 0f - num868;
                }
            }
            else if (/*Projectile.directionY == 1 &&*/ Projectile.velocity.Y < num868)
            {
                Projectile.velocity.Y += 0.04f;
                if (Projectile.velocity.Y < 0f - num868)
                {
                    Projectile.velocity.Y += 0.05f;
                }
                else if (Projectile.velocity.Y < 0f)
                {
                    Projectile.velocity.Y -= 0.03f;
                }
                if (Projectile.velocity.Y > num868)
                {
                    Projectile.velocity.Y = num868;
                }
            }

        }

        #region Frames
        public void Visuals()
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Projectile[Projectile.type].Value.Height / Projectile.frameCounter;
            }
            if (Projectile.velocity.X < 0)
            {
                Projectile.spriteDirection = -1;
            }
            else
            {
                Projectile.spriteDirection = 1;
            }
            Projectile.rotation = Projectile.velocity.X * 0.08f;
            Projectile.frameCounter += 1;
            if (Projectile.frameCounter >= 4.0)
            {
                Projectile.frameCounter = 0;
            }

            if (chargeTimer3 == 0)
            {
                Projectile.alpha = 0;
            }
            else
            {
                Projectile.alpha = 200;
            }
        }
        #endregion



        /* what the hell IS Projectile? i cant find anything about it
         public int MagicDefenseValue() 
            { 
                return 5;
            } 
          
         */
    }
}
