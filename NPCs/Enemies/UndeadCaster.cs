using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using static tsorcRevamp.SpawnHelper;
using Microsoft.Xna.Framework.Graphics;

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
            npc.CloneDefaults(NPCID.GoblinSorcerer);
            Main.npcFrameCount[npc.type] = 2;
            aiType = NPCID.GoblinSorcerer;
            npc.lifeMax = 30;
            npc.damage = 15;
            npc.scale = 1f;
            npc.knockBackResist = 0.1f;
            npc.value = 650;
            npc.defense = 10;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.UndeadCasterBanner>();
            npc.height = 44;
            npc.width = 28;
            npc.HitSound = SoundID.NPCHit2;
            npc.DeathSound = SoundID.NPCDeath2;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {

            float chance = 0;
            Player p = spawnInfo.player;
            if (p.ZoneRockLayerHeight && Main.dayTime) return 0.0285f;
            if (p.ZoneRockLayerHeight && !Main.dayTime) return 0.05f;
            if (!Main.hardMode && Sky(p) && !Main.dayTime) return 0.033f;
            if (!Main.hardMode && (spawnInfo.player.ZoneRockLayerHeight && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson)) && !Main.dayTime) return 0.033f;

            return chance;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 27, 2 * hitDirection, -1.75f);
                }
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));

                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 1"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
            }
        }

        public override void NPCLoot()
        {
            Player player = Main.player[npc.target];

            Item.NewItem(npc.getRect(), ItemID.Heart, 1);
            Item.NewItem(npc.getRect(), ItemID.Heart, 1);
            Item.NewItem(npc.getRect(), ItemID.Heart, 1);
            Item.NewItem(npc.getRect(), ItemID.Diamond, Main.rand.Next(1, 3));

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
            }
            else
            {
                Item.NewItem(npc.getRect(), ItemID.HealingPotion, 2);
            }


            if (Main.rand.NextFloat() <= .20f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WoodenWand"), 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WandOfDarkness"), 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .05f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WandOfFire"), 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .12f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("AttraidiesRelic"));
            }
            if (Main.rand.NextFloat() <= .05f) //lol dead chicken as rare as a fire wand
            {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(npc.getRect(), ItemID.SpellTome);
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D glowTexture = mod.GetTexture("NPCs/Enemies/UndeadCaster_Glow");
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (npc.spriteDirection == 1)
            {
                spriteBatch.Draw(glowTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 50, 68), lightColor, npc.rotation, new Vector2(25, 42), npc.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(glowTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 50, 68), lightColor, npc.rotation, new Vector2(25, 42), npc.scale, effects, 0f);
            }

        }

        public override void FindFrame(int frameHeight)
        {
            npc.spriteDirection = npc.direction;

            if (npc.ai[1] > 0f)
            {
                npc.frame.Y = 1 * frameHeight;
            }
            else
            {
                npc.frame.Y = 0 * frameHeight;
            }
        }
    }
}