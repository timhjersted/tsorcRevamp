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
    class TheSorrow : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 26600;
            NPC.damage = 95;
            NPC.defense = 20;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 150000;
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
            despawnHandler = new NPCDespawnHandler("The Sorrow claims you...", Color.DarkCyan, 29);
        }

        //npc.ai[0] = damage taken counter
        //npc.ai[1] = invulnerability timer
        //npc.ai[3] = state counter
        int hitTime = 0; //How long since it's last been hit (used for reducing damage counter)
        int waterTrailsDamage = 45;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = NPC.damage / 2;
            NPC.defense = NPC.defense += 12;
            NPC.lifeMax = 28000;
            waterTrailsDamage = (int)(waterTrailsDamage * 1.3 / 2);
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
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 0.5f + (15.5f * (NPC.ai[0] / (NPC.lifeMax / 10))));
            Main.dust[dust].noGravity = true;


            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
                NPC.dontTakeDamage = false;
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
                        //If the sorrow doesn't have line of sight to the player due to blocks in the way, its projectiles will be able to phase through walls to hit them and travel much faster.
                        //phasedBullets is passed to the projectile's ai[0] value (which takes a float) to tell it whether or not to collide with tiles
                        float speed = 9f;
                        float phasedBullets = 0;
                        if (!Collision.CanHit(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) && !Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1))
                        {
                            speed = 18f;
                            phasedBullets = 1;
                        }

                        int type = ModContent.ProjectileType<WaterTrail>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);

                        //Get the vector that points from the NPC to the player
                        Vector2 difference = Main.player[NPC.target].Center - NPC.Center;

                        //Create a new vector that is just a line with length [speed], and rotate it to be facing the player based on the preivious vector
                        Vector2 velocity = new Vector2(speed, 0).RotatedBy(difference.ToRotation());
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            //Fire a projectile right at the player
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, velocity.X, velocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //Rotate it further to fire the shots angled away from the player
                            Vector2 angledVelocity = velocity.RotatedBy(Math.PI / 6);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 6);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //And again the more offset shots
                            angledVelocity = velocity.RotatedBy(Math.PI / 3);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 3);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);

                            //And once mroe for the most offset shots
                            angledVelocity = velocity.RotatedBy(Math.PI / 1.8);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            angledVelocity = velocity.RotatedBy(-Math.PI / 1.8);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, angledVelocity.X, angledVelocity.Y, type, waterTrailsDamage, 0f, Main.myPlayer, phasedBullets);
                            //Could this all have been a for loop? Yeah. Easier to read like this though, imo.
                        }
                        NPC.ai[1] = -180;
                    }
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 690)
                {
                    //Then chill for a second.
                    //This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 132, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, default, 1f);
                }
                else if (NPC.ai[2] >= 690 && NPC.ai[2] < 1290)
                {
                    int dashSpeed = 18;
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((vector8.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (vector8.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * dashSpeed) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * dashSpeed) * -1;
                    }
                }
                else NPC.ai[2] = 0;
            }
            else
            {
                NPC.ai[3]++;
                NPC.alpha = 200;
                NPC.dontTakeDamage = true;
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
                    float num48 = 13f;
                    float invulnDamageMult = 1.2f;
                    int type = ModContent.ProjectileType<WaterTrail>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    //yes do it manually. im not using a loop. i don't care //Understandable, have a nice day.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1), (float)((Math.Sin(rotation + 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.8) * num48) * -1), (float)((Math.Sin(rotation - 0.4) * num48) * -1), type, (int)(waterTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                    }
                    NPC.ai[1] = -180;
                }
                if (NPC.ai[3] == 100)
                {
                    NPC.ai[3] = 1;
                    NPC.life += 200; //amount boss heals when going invisible
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int i = 0; i < 40; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, 0, 0, 0, new Color(), 3f);
                    }
                }
            }
        }

        public override void FindFrame(int frameHeight)
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
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            hitTime = 0;
            NPC.ai[0] += (float)damage;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                NPC.ai[3] = 1; //begin invulnerability state
                for (int i = 0; i < 50; i++)
                { //dustsplosion on invulnerability
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 29, 0, 0, 100, default, 3f);
                }
                NPC.ai[1] = -180;
                NPC.ai[0] = 0; //reset damage counter
            }
            return true;
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheSorrowBag>()));
        }
        public override void OnKill()
        {
            for (int num36 = 0; num36 < 100; num36++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 29, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int num36 = 0; num36 < 100; num36++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 132, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CrestOfWater>(), 2);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.AdamantiteDrill, 1, false, -1);
            }
        }
    }
}