using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    //[AutoloadBossHead]
    class TheHunterChild : ModNPC
    {
        int sproutDamage = 33;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] 
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        } 
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 6000;
            NPC.damage = 53;
            NPC.defense = 36;
            NPC.knockBackResist = 0f;
            NPC.value = 31500;
            NPC.npcSlots = 1;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            DrawOffsetY = +70;
            NPC.width = 140;
            NPC.height = 60;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.TheHunterChild.DespawnHandler"), Color.Green, 89);
        }

        int hitTime = 0;
        public float flapWings;

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
                return false;   
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) 
        {
            target.AddBuff(BuffID.Bleeding, 30 * 60, false);
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
            //dusts get bigger as damage taken increases
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));

            Player player = Main.player[NPC.target];
            if (player.HasBuff(BuffID.Hunter) || player.HasItem(ModContent.ItemType<Items.Potions.PermanentPotions.PermanentHunterPotion>()))
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 89, NPC.velocity.X, NPC.velocity.Y, 200, default, 0.01f + (5.5f * (NPC.ai[0] / (NPC.lifeMax / 10)))); //.1 was 0.5f
                Main.dust[dust].noGravity = true;
            }

            flapWings++;

            //Flap Wings
            
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.7f }, NPC.position); //wing flap sound
            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.8f }, NPC.position);
                flapWings = 0;
            }

            //getting close triggers hunter vision
            if (NPC.Distance(player.Center) < 80)
            {
                player.AddBuff(BuffID.Hunter, 60, false);
            }

            bool hunterAlive = NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheHunter>());

            
            if (!hunterAlive)
            {
                if(NPC.life > 500)
                { NPC.life = 500; } 
            }

            if (NPC.ai[3] == 0)
            {
                //NPC.alpha = 200;
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
                        float num48 = 3f; //was 14f

                        int type = ModContent.ProjectileType<MiracleSprouter>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass, vector8);
                        float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 250;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 250;
                            projIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, sproutDamage, 0f, Main.myPlayer);
                            Main.projectile[projIndex].timeLeft = 250;
                        }
                        NPC.ai[1] = -90;
                    }
                    NPC.netUpdate = true; //new
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 950)
                {
                    //Then chill for a few seconds.
                    //This exists to delay switching to the 'charging' pattern for 150 frames, because otherwise the way the sprouters linger can often make the first charge impossible to dodge
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 131, Main.rand.Next(-1, 1), Main.rand.Next(-10, 1), 200, default, 0.5f);
                }
                else if (NPC.ai[2] >= 950 && NPC.ai[2] < 1350)
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
                //NPC.alpha = 210;
                NPC.defense = 66;
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
                    float num48 = 15f;//was 22
                    float invulnDamageMult = 1.7f; //was 1.3
                    int type = ModContent.ProjectileType<MiracleSprouter>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, vector8);
                    float rotation = (float)Math.Atan2(vector8.Y - 80 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 150;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation + 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 150;
                        num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y - 80, (float)((Math.Cos(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.X, (float)((Math.Sin(rotation - 0.4) * num48) * -1) + Main.player[NPC.target].velocity.Y, type, (int)(sproutDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 150;
                    }
                    NPC.ai[1] = -90;
                }
                if (NPC.ai[3] == 100)
                {
                    if (NPC.ai[3] == 100)
                
                    
                    NPC.ai[3] = 1;
                    NPC.life -= 500;
                    //loses health on enrage now, as an inversion of usual mechanic, but enraged miracle sprouter attack has a higher damage multiplier to add a risk/reward element
                    if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                }
                if (NPC.ai[1] >= 0)
                {
                    NPC.ai[3] = 0;
                    for (int num36 = 0; num36 < 40; num36++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 0, default, 0.3f);//was 3f
                    }
                }
            }
        }
        public override void FindFrame(int currentFrame)
        {
            Player player = Main.player[NPC.target];

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
                
                if (player.HasBuff(BuffID.Hunter) || player.HasItem(ModContent.ItemType<Items.Potions.PermanentPotions.PermanentHunterPotion>()))
                { 
                    NPC.alpha = 50; 
                }
                else
                { 
                    NPC.alpha = 255; 
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            }
            else
            {
                if (player.HasBuff(BuffID.Hunter) || player.HasItem(ModContent.ItemType<Items.Potions.PermanentPotions.PermanentHunterPotion>()))
                { 
                    NPC.alpha = 150; 
                }
                else
                { 
                    NPC.alpha = 255; 
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            hitTime = 0;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                NPC.ai[3] = 1;
                Color color = new Color();
                for (int num36 = 0; num36 < 50; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, color, 0.5f);
                }
                for (int num36 = 0; num36 < 20; num36++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, color, 0.5f);
                }
                NPC.ai[1] = -200;
                NPC.ai[0] = 0;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
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
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 131, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.DarkGreen, 3f);
            }

        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                if (NPC.life <= 0)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Child_Gore_1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Child_Gore_2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Child_Gore_3").Type, 1f);
                }
            }
        }
    }
}
