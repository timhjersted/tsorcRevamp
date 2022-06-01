using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static tsorcRevamp.SpawnHelper;

namespace tsorcRevamp.NPCs.Enemies
{
    class UndeadCaster : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Undead Caster");
            //Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GoblinSorcerer];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.GoblinSorcerer);
            Main.npcFrameCount[NPC.type] = 2;
            AIType = NPCID.GoblinSorcerer;
            NPC.lifeMax = 30;
            NPC.damage = 15;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.1f;
            NPC.value = 650;
            NPC.defense = 10;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.UndeadCasterBanner>();
            NPC.height = 44;
            NPC.width = 28;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath2;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            float chance = 0;
            Player p = spawnInfo.Player;
            if (!Main.hardMode && p.ZoneRockLayerHeight && !spawnInfo.Player.ZoneCrimson && Main.dayTime) return 0.0285f;
            if (!Main.hardMode && p.ZoneRockLayerHeight && !spawnInfo.Player.ZoneCrimson && !Main.dayTime) return 0.05f;
            if (!Main.hardMode && Sky(p) && !Main.dayTime) return 0.033f;
            if (!Main.hardMode && (spawnInfo.Player.ZoneRockLayerHeight && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) && !Main.dayTime) return 0.033f;

            return chance;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 27, 2 * hitDirection, -1.75f);
                }
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));

                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Undead Caster Gore 3").Type, 1f);
            }
        }

        public override void OnKill()
        {
            Player player = Main.player[NPC.target];

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Diamond, Main.rand.Next(1, 3));
            if (Main.rand.Next(8) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.Next(8) == 0)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
            }
            else
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HealingPotion, 2);
            }


            if (Main.rand.NextFloat() <= .20f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("WoodenWand").Type, 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("WandOfDarkness").Type, 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .05f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("WandOfFire").Type, 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .12f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("AttraidiesRelic").Type);
            }
            if (Main.rand.NextFloat() <= .05f) //lol dead chicken as rare as a fire wand
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("DeadChicken").Type);
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.SpellTome);
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D glowTexture = Mod.GetTexture("NPCs/Enemies/UndeadCaster_Glow");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (NPC.spriteDirection == 1)
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 50, 68), lightColor, NPC.rotation, new Vector2(25, 42), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(glowTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 50, 68), lightColor, NPC.rotation, new Vector2(25, 42), NPC.scale, effects, 0f);
            }

        }

        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = NPC.direction;

            if (NPC.ai[1] > 0f)
            {
                NPC.frame.Y = 1 * frameHeight;
            }
            else
            {
                NPC.frame.Y = 0 * frameHeight;
            }
        }
    }
}