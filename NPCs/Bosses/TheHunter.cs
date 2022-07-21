using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheHunter : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 30800;
            NPC.damage = 120;
            NPC.defense = 26;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 220000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            DrawOffsetY = +70;
            NPC.width = 140;
            NPC.height = 60;

            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("The Hunter seeks its next prey...", Color.Green, 89);
        }

        int hitTime = 0;
        int sproutDamage = 60; 
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = NPC.damage / 2;
            NPC.defense = NPC.defense += 10;
            NPC.lifeMax = 35000;
            sproutDamage = (int)(sproutDamage * 1.3 / 2);
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.netUpdate = true;
            NPC.ai[2]++;
            NPC.ai[1]++;
            hitTime++;
            if (NPC.ai[0] > 0) NPC.ai[0] -= hitTime / 10;
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 89, NPC.velocity.X, NPC.velocity.Y, 200, default, 0.5f + (15.5f * (NPC.ai[0] / (NPC.lifeMax / 10))));
            Main.dust[dust].noGravity = true;

            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
                //NPC.dontTakeDamage = false;
                NPC.defense = 26;
                if (NPC.ai[2] < 600)
                {
                    if (Main.player[NPC.target].position.X < vector8.X)
                    {
                        if (NPC.velocity.X > -8) { NPC.velocity.X -= 0.22f; }
                    }
                    if (Main.player[NPC.target].position.X > vector8.X)
                    {
                        if (NPC.velocity.X < 8) { NPC.velocity.X += 0.22f; }
                    }

                    if (Main.player[NPC.target].position.Y < vector8.Y + 300)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].position.Y > vector8.Y + 300)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }

                    if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                    {
                        float num48 = 14f;

                        int type = ModContent.ProjectileType<MiracleSprouter>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                        float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 50;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 50;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 50;
                        }
                        NPC.ai[1] = -90;
                    }
                    NPC.netUpdate = true; //new
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 750)
                {
                    //Then chill for a few seconds.
                    //This exists to delay switching to the 'charging' pattern for 150 frames, because otherwise the way the sprouters linger can often make the first charge impossible to dodge
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 131, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);
                }
                else if (NPC.ai[2] >= 750 && NPC.ai[2] < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (vector8.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = ((float)(Math.Cos(rotation) * 25) * -1) + Main.player[NPC.target].velocity.X;
                        NPC.velocity.Y = ((float)(Math.Sin(rotation) * 25) * -1) + Main.player[NPC.target].velocity.Y;
                    }
                }
                else NPC.ai[2] = 0;
            }
            else
            {
                NPC.ai[3]++;
                NPC.alpha = 200;
                NPC.defense = 100;
                //NPC.dontTakeDamage = true;
                if (Main.player[NPC.target].position.X < vector8.X)
                {
                    if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                }
                if (Main.player[NPC.target].position.X > vector8.X)
                {
                    if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                }
                if (Main.player[NPC.target].position.Y < vector8.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                    else NPC.velocity.Y -= 0.07f;
                }
                if (Main.player[NPC.target].position.Y > vector8.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                    else NPC.velocity.Y += 0.07f;
                }
                if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                {
                    float num48 = 22f;
                    float invulnDamageMult = 1.3f;
                    int type = ModContent.ProjectileType<MiracleSprouter>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 50;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 50;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 50;
                    }
                    NPC.ai[1] = -90;
                }
                if (NPC.ai[3] == 100)
                {
                    NPC.ai[3] = 1;
                    NPC.life += 200;
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int num36 = 0; num36 < 40; num36++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 0, default, 3f);
                    }
                }
            }
        }
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            hitTime = 0;
            NPC.ai[0] += (float)damage;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                NPC.ai[3] = 1;
                Color color = new Color();
                for (int num36 = 0; num36 < 50; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, color, 3f);
                }
                for (int num36 = 0; num36 < 20; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, color, 3f);
                }
                NPC.ai[1] = -200;
                NPC.ai[0] = 0;
            }
            return true;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheHunterBag>()));
        }

        public override void OnKill()
        {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 89, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 100; num36++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 131, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CrestOfEarth>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.WaterWalkingBoots, 1, false, -1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Drax, 1, false, -1);
            }
        }
    }
}
