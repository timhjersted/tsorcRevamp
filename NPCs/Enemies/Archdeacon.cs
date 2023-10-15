using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class Archdeacon : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 20;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 500;
            NPC.scale = 1.2f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 4000; // was 375
            NPC.width = 28;
            NPC.knockBackResist = 0.1f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ArchdeaconBanner>();
        }



        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.ZoneSkyHeight && spawnInfo.Player.ZoneSnow && Main.hardMode && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.Archdeacon>()) < 3 && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.AttraidiesIllusion>()) < 1
               && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.BarrowWight>()) < 3 && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())))
            {
                chance += 0.35f;
            }
            if (spawnInfo.Player.ZoneSkyHeight && Main.hardMode && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.AttraidiesIllusion>()) < 1
               && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.BarrowWight>()) < 3 && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())))
            {
                chance += 0.2f;
            }
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())))
            {
                chance += 0.15f;
            }
            return chance;
        }

        public override void AI()
        {

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots


            bool validTarget = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);


            if (NPC.life > NPC.lifeMax * 3 / 10)
            {
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 200, Color.LightCyan, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= NPC.lifeMax * 3 / 10)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.DarkCyan, 2f);
                Main.dust[dust].noGravity = true;
            }

            //Bubble Attack & Hold Attack
            if (NPC.ai[1] >= 60)
            {
                int choice = Main.rand.Next(6);

                if (choice >= 1)
                {
                    if (NPC.ai[0] >= 12 && NPC.ai[2] < 3 && Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 220) //22 was 12
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float num48 = 3f;
                            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                            int damage = 22;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.Bubble>();
                            float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                            int proj = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, Main.myPlayer);
                            Main.projectile[proj].timeLeft = 180;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, PitchVariance = 2f }, NPC.Center);
                        NPC.ai[0] = 0;
                        NPC.ai[2]++;
                    }
                }
                if (choice == 0)
                {
                    if (NPC.ai[0] >= 12 && NPC.ai[2] < 1 && Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > 250)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float num48 = 5f;
                            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                            int damage = 16;
                            int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellHoldBall>();
                            float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                            int proj = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, Main.myPlayer);
                            Main.projectile[proj].timeLeft = 420;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, PitchVariance = 2f }, NPC.Center);
                        NPC.ai[0] = 0;
                        NPC.ai[2]++;


                    }

                }


            }

            if (NPC.ai[1] >= 20)
            {
                NPC.velocity.X *= 0.17f;
                NPC.velocity.Y *= 0.03f;//was.17
            }

            if ((NPC.ai[1] >= 270 && NPC.life > 150) || (NPC.ai[1] >= 190 && NPC.life <= 150))//was 260 and 180
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = false;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                NPC.ai[1] = 0;
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                {
                    NPC.TargetClosest(true);
                }
                if (Main.player[NPC.target].dead)
                {
                    NPC.position.X = 0;
                    NPC.position.Y = 0;
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 0;
                        return;
                    }
                }
                else
                {
                    tsorcRevampAIs.QueueTeleport(NPC, 38, true, 60);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                }
            }
            //end region teleportation


            NPC.ai[3]++;
            Player Player = Main.player[NPC.target];
            //if (Main.netMode != NetmodeID.Server)
            //{ 
            if (NPC.ai[3] >= 200) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.NextBool(2) && Player.position.Y < NPC.position.Y && NPC.Distance(Player.Center) < 400) //1 in 2chance boss will use attack when player is above enemy
                {

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float num48 = 1f;
                        Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                        int damage = 22;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemyIceBallUp>();
                        float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        int proj = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, Main.myPlayer);
                        Main.projectile[proj].timeLeft = 1;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.AncientLight, NPC.velocity.X, NPC.velocity.Y, 0, Color.Black, 2f);
                        Main.dust[dust].noGravity = true;
                    }
                    NPC.ai[3] = 0;

                }
            }
            //}
            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 600)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }




        public override void FindFrame(int frameHeight)
        {
            if ((NPC.velocity.X != 0 || NPC.velocity.Y != 0) || (NPC.ai[0] >= 12 && NPC.ai[2] < 5))
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            else
            {
                NPC.frame.Y = 0 * frameHeight;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<HealingElixir>(), 10, 1, 1, 3));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 25));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 20));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                }
            }
        }
    }
}
