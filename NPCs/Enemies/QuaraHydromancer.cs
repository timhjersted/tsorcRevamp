using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.NPCs.Enemies
{
    class QuaraHydromancer : ModNPC
    {

        int bubbleDamage = 60;
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 15;
            AnimationType = 21;
            NPC.aiStyle = 3;
            NPC.damage = 65;
            NPC.defense = 22;
            NPC.height = 45;
            NPC.lifeMax = 500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1500;
            NPC.width = 18;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.25f;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.QuaraHydromancerBanner>();

            if (Main.hardMode) { NPC.lifeMax = 1000; NPC.defense = 22; NPC.damage = 125; NPC.value = 1500; bubbleDamage = 70; }
            if (tsorcRevampWorld.SuperHardMode) { NPC.lifeMax = 3000; NPC.defense = 50; NPC.damage = 160; NPC.value = 3600; bubbleDamage = 80; }
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            bubbleDamage = (int)(bubbleDamage / 2);
        }



        float bubbleTimer;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;

            if (spawnInfo.Water) return 0f;

            //now spawns in hallow, since jungle was getting crowded
            //spawns more before the rage is defeated

            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && P.ZoneOverworldHeight && Main.rand.NextBool(30)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && !Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(25)) return 1;
            if (Main.hardMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && Main.dayTime && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(35)) return 1;
            if (Main.hardMode && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && P.ZoneHallow && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.NextBool(10)) return 1;
            if (Main.hardMode && spawnInfo.Lihzahrd && Main.rand.NextBool(45)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDesert && Main.rand.NextBool(45)) return 1;
            if (tsorcRevampWorld.SuperHardMode && P.ZoneHallow && Main.rand.NextBool(10)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneGlowshroom && Main.rand.NextBool(5)) return 1;
            return 0;
        }
        #endregion

        int inkJetCooldown = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 2, 0.05f, canTeleport: false, lavaJumping: true);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0);
            tsorcRevampAIs.SimpleProjectile(NPC, ref bubbleTimer, 80, ModContent.ProjectileType<Projectiles.Enemy.Bubble>(), bubbleDamage, 6, lineOfSight, true, SoundID.Item87, 0); //2, 87 is bubble 2 sound

            if (Main.GameUpdateCount % 600 == 0 && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.Fiends.WaterFiendKraken>())) & Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.InkGeyser>(), bubbleDamage, 0, Main.myPlayer);
                inkJetCooldown = 120;
            }

            if (inkJetCooldown > 0)
            {
                NPC.velocity = Vector2.Zero;
                inkJetCooldown--;
            }


            //projectile sound needs volume and pitch variables (the last two below)
            //if (bubbleTimer >= 80)
            //{
            //	Terraria.Audio.SoundEngine.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 87, 0.2f, -0.1f); // water2 sound
            //}


            //PLAY CREATURE SOUND
            if (Main.rand.NextBool(1000))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.3f }, NPC.Center); // water sound
            }


            //JUSTHIT CODE

            Player player2 = Main.player[NPC.target];
            if (NPC.justHit && NPC.Distance(player2.Center) < 100)
            {
                bubbleTimer = 0f;
            }
            if (NPC.justHit && NPC.Distance(player2.Center) < 150 && Main.rand.NextBool(2))
            {
                bubbleTimer = 40f;
                NPC.velocity.Y = Main.rand.NextFloat(-11f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-4f, -3f);
                NPC.netUpdate = true;
            }
            if (NPC.justHit && NPC.Distance(player2.Center) > 200 && Main.rand.NextBool(2))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-11f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(4f, 3f);
                NPC.netUpdate = true;
            }

            //TELEGRAPH DUST
            if (bubbleTimer >= 40)
            {
                Lighting.AddLight(NPC.Center, Color.Blue.ToVector3());

                for (int j = 0; j < bubbleTimer - 39; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(48, 64);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = dir * -1;
                    dustVel.Normalize();
                    dustVel *= 3;
                    Dust.NewDustPerfect(dustPos, 29, dustVel, 200).noGravity = true;
                }
            }
        }



        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 1").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
            }
            //if (Main.rand.Next(99) < 10) Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ItemID.HealingPotion, 1);
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Magic.GreatEnergyBeamScroll>(), 50));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.ManaRegenerationPotion, 20));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.SwiftnessPotion, 20));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.WaterWalkingPotion, 33));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.BattlePotion, 33));
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
        }
    }
}