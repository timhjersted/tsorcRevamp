using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.JungleWyvern
{
    [AutoloadBossHead]
    class JungleWyvernHead : ModNPC
    {

        int breathCD = 180;
        bool breath = false;
        int juvenileSpawnTimer = 0;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ancient Jungle Wyvern");
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = 6;
            NPC.netAlways = true;
            NPC.npcSlots = 1;
            NPC.width = 45;
            NPC.height = 45;
            NPC.timeLeft = 22000;
            NPC.damage = 80;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit7;
            NPC.DeathSound = SoundID.NPCDeath8;
            NPC.lifeMax = 24000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.value = 90000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("The Jungle Wyvern departs to seek its next prey...", Color.GreenYellow, DustID.GreenFairy);

        }

        public int CursedFlamesDamage = 22;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            //spawn body
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[0] == 0f)
            {
                NPC.ai[2] = NPC.whoAmI;
                NPC.realLife = NPC.whoAmI;
                int num119 = NPC.whoAmI;
                for (int i = 0; i < 22; i++)
                {
                    int npcType = ModContent.NPCType<JungleWyvernBody>();
                    switch (i)
                    {
                        //2 body parts (0-1)
                        case 2: //legs
                                //4 body parts (3-6)
                        case 7: //legs etc
                        case 12:
                        case 17:
                            npcType = ModContent.NPCType<JungleWyvernLegs>();
                            break;
                        case 19:
                            npcType = ModContent.NPCType<JungleWyvernBody2>();
                            break;
                        case 20:
                            npcType = ModContent.NPCType<JungleWyvernBody3>();
                            break;
                        case 21:
                            npcType = ModContent.NPCType<JungleWyvernTail>();
                            break;
                    }
                    int num122 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + NPC.width / 2), (int)(NPC.position.Y + (float)NPC.height), npcType, NPC.whoAmI);
                    Main.npc[num122].ai[2] = NPC.whoAmI;
                    Main.npc[num122].realLife = NPC.whoAmI;
                    Main.npc[num122].ai[1] = num119;
                    Main.npc[num119].ai[0] = num122;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num122);
                    num119 = num122;
                }
            }
            if (NPC.CountNPCS(Mod.Find<ModNPC>("JungleWyvernJuvenileHead").Type) < 2)
            {
                juvenileSpawnTimer += Main.rand.Next(1, 3);
            }


            if (juvenileSpawnTimer >= 1900 && NPC.CountNPCS(Mod.Find<ModNPC>("JungleWyvernJuvenileHead").Type) < 2) //1900 was 1200
            {
                if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 500)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + Main.rand.Next(-20, 20), (int)NPC.position.Y + Main.rand.Next(-20, 20), Mod.Find<ModNPC>("JungleWyvernJuvenileHead").Type);
                    }
                    juvenileSpawnTimer = 0;
                }
            }

            if (Main.rand.Next(240) == 0) //was 120
            {
                breath = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
                NPC.netUpdate = true;
            }

            if (breath)
            {

                float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {

                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y /*+ (5f * npc.direction)*/, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 2), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), CursedFlamesDamage, 0f, Main.myPlayer); //cursed dragons breath
                    Main.projectile[num54].timeLeft = 20;//was 25
                    Main.projectile[num54].scale = 0.5f;

                }
                NPC.netUpdate = true;


                breathCD--;

            }

            if (breathCD <= 0)
            {
                breath = false;
                breathCD = 100;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20);
            }

            if (NPC.velocity.X < 0f)
            {
                NPC.spriteDirection = 1;
            }
            if (NPC.velocity.X > 0f)
            {
                NPC.spriteDirection = -1;
            }
            float num111 = 12f;
            float Acceleration = 0.15f;
            Vector2 vector14 = new Vector2(NPC.position.X + NPC.width * 0.5f, NPC.position.Y + NPC.height * 0.5f);
            float num113 = Main.rand.Next(-500, 500) + Main.player[NPC.target].position.X + Main.player[NPC.target].width / 2;
            float num114 = Main.rand.Next(-500, 500) + Main.player[NPC.target].position.Y + Main.player[NPC.target].height / 2;
            num113 = (int)(num113 / 16f) * 16;
            num114 = (int)(num114 / 16f) * 16;
            vector14.X = (int)(vector14.X / 16f) * 16;
            vector14.Y = (int)(vector14.Y / 16f) * 16;
            num113 -= vector14.X;
            num114 -= vector14.Y;
            float num115 = (float)Math.Sqrt(num113 * num113 + num114 * num114);
            float num116 = Math.Abs(num113);
            float num117 = Math.Abs(num114);
            float num118 = num111 / num115;
            num113 *= num118;
            num114 *= num118;
            bool flee = false;
            if (((NPC.velocity.X > 0f && num113 < 0f) || (NPC.velocity.X < 0f && num113 > 0f) || (NPC.velocity.Y > 0f && num114 < 0f) || (NPC.velocity.Y < 0f && num114 > 0f)) && Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y) > Acceleration / 2f && num115 < 300f)
            {
                flee = true;
                if (Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y) < num111)
                {
                    NPC.velocity *= 1.1f;
                }
            }
            if (NPC.position.Y > Main.player[NPC.target].position.Y || Main.player[NPC.target].dead)
            {
                flee = true;
                if (Math.Abs(NPC.velocity.X) < num111 / 2f)
                {
                    if (NPC.velocity.X == 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X - (float)NPC.direction;
                    }
                    NPC.velocity.X = NPC.velocity.X * 1.1f;
                }
                else if (NPC.velocity.Y > 0f - num111)
                {
                    NPC.velocity.Y = NPC.velocity.Y - Acceleration;
                }
            }
            if (!flee)
            {
                if ((NPC.velocity.X > 0f && num113 > 0f) || (NPC.velocity.X < 0f && num113 < 0f) || (NPC.velocity.Y > 0f && num114 > 0f) || (NPC.velocity.Y < 0f && num114 < 0f))
                {
                    if (NPC.velocity.X < num113)
                    {
                        NPC.velocity.X = NPC.velocity.X + Acceleration;
                    }
                    else if (NPC.velocity.X > num113)
                    {
                        NPC.velocity.X = NPC.velocity.X - Acceleration;
                    }
                    if (NPC.velocity.Y < num114)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + Acceleration;
                    }
                    else if (NPC.velocity.Y > num114)
                    {
                        NPC.velocity.Y = NPC.velocity.Y - Acceleration;
                    }
                    if ((double)Math.Abs(num114) < (double)num111 * 0.2 && ((NPC.velocity.X > 0f && num113 < 0f) || (NPC.velocity.X < 0f && num113 > 0f)))
                    {
                        if (NPC.velocity.Y > 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y + Acceleration * 2f;
                        }
                        else
                        {
                            NPC.velocity.Y = NPC.velocity.Y - Acceleration * 2f;
                        }
                    }
                    if ((double)Math.Abs(num113) < (double)num111 * 0.2 && ((NPC.velocity.Y > 0f && num114 < 0f) || (NPC.velocity.Y < 0f && num114 > 0f)))
                    {
                        if (NPC.velocity.X > 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X + Acceleration * 2f;
                        }
                        else
                        {
                            NPC.velocity.X = NPC.velocity.X - Acceleration * 2f;
                        }
                    }
                }
                else if (num116 > num117)
                {
                    if (NPC.velocity.X < num113)
                    {
                        NPC.velocity.X = NPC.velocity.X + Acceleration * 1.1f;
                    }
                    else if (NPC.velocity.X > num113)
                    {
                        NPC.velocity.X = NPC.velocity.X - Acceleration * 1.1f;
                    }
                    if ((double)(Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y)) < (double)num111 * 0.5)
                    {
                        if (NPC.velocity.Y > 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y + Acceleration;
                        }
                        else
                        {
                            NPC.velocity.Y = NPC.velocity.Y - Acceleration;
                        }
                    }
                }
                else
                {
                    if (NPC.velocity.Y < num114)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + Acceleration * 1.1f;
                    }
                    else if (NPC.velocity.Y > num114)
                    {
                        NPC.velocity.Y = NPC.velocity.Y - Acceleration * 1.1f;
                    }
                    if ((double)(Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y)) < (double)num111 * 0.5)
                    {
                        if (NPC.velocity.X > 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X + Acceleration;
                        }
                        else
                        {
                            NPC.velocity.X = NPC.velocity.X - Acceleration;
                        }
                    }
                }
            }
            NPC.rotation = (float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X) + 1.57f;
            if (Main.rand.Next(3) == 0)
            {
                int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 62, 0f, 0f, 100, Color.White, 2f);
                Main.dust[dust].noGravity = true;
            }
            if (NPC.life <= 0)
            {
                NPC.active = false;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 origin = new Vector2(TextureAssets.Npc[NPC.type].Value.Width / 2, TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type] / 2);
            Color alpha = Color.White;
            SpriteEffects effects = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                effects = SpriteEffects.FlipHorizontally;
            }
            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, new Vector2(NPC.position.X - Main.screenPosition.X + NPC.width / 2 - (float)TextureAssets.Npc[NPC.type].Value.Width * NPC.scale / 2f + origin.X * NPC.scale, NPC.position.Y - Main.screenPosition.Y + (float)NPC.height - (float)TextureAssets.Npc[NPC.type].Value.Height * NPC.scale / (float)Main.npcFrameCount[NPC.type] + 4f + origin.Y * NPC.scale + 56f), NPC.frame, alpha, NPC.rotation, origin, NPC.scale, effects, 0f);
            NPC.alpha = 255;
            return true;
        }

        private static int ClosestSegment(NPC head, params int[] segmentIDs)
        {
            List<int> segmentIDList = new List<int>(segmentIDs);
            Vector2 targetPos = Main.player[head.target].Center;
            int closestSegment = head.whoAmI; //head is default, updates later
            float minDist = 1000000f; //arbitrarily large, updates later
            for (int i = 0; i < Main.npc.Length; i++)
            { //iterate through every NPC
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && segmentIDList.Contains(npc.type))
                { //if the npc is part of the wyvern
                    float targetDist = (npc.Center - targetPos).Length();
                    if (targetDist < minDist)
                    { //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
                        minDist = targetDist; //update minDist. future iterations will compare against the updated value
                        closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
                    }
                }
            }
            return closestSegment; //the whoAmI of the closest segment
        }

        public override bool SpecialNPCLoot()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>())
                {
                    Main.npc[i].active = false;
                }
            }
            int closestSegmentID = ClosestSegment(NPC, ModContent.NPCType<JungleWyvernBody>(), ModContent.NPCType<JungleWyvernBody2>(), ModContent.NPCType<JungleWyvernBody3>(), ModContent.NPCType<JungleWyvernTail>());
            NPC.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot
            return false;
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
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.JungleWyvernBag>()));
        }
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            damage *= 2;
            base.OnHitByItem(player, item, damage, knockback, crit);
        }
        public override void OnKill()
        {

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.ChloranthyRing>(), 1, false, -1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Sapphire, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Ruby, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Topaz, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Diamond, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Emerald, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Amethyst, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Amethyst, Main.rand.Next(2, 10));
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.NecroHelmet);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.NecroBreastplate);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.NecroGreaves);
            if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<JungleWyvernHead>())))
            { //If the boss has not yet been killed
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<DarkSoul>(), 9000); //Then drop the souls
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.StaminaVessel>());

            }
        }
    }
}
