using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    class UndeadCaster : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Undead Caster");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GoblinSorcerer];
        }

        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.GoblinSorcerer);
            animationType = NPCID.GoblinSorcerer;
            aiType = NPCID.GoblinSorcerer;
            npc.lifeMax = 50;
            npc.damage = 30;
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
            if (spawnInfo.player.ZoneRockLayerHeight)
            {
                chance = .03f;
            }

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
            Item.NewItem(npc.getRect(), ItemID.Heart, 3);
            Item.NewItem(npc.getRect(), ItemID.Diamond, Main.rand.Next(1, 3));
            Item.NewItem(npc.getRect(), ItemID.HealingPotion, 2);

            if (Main.rand.NextFloat() <= .20f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WoodenWand"));
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WandOfDarkness"));
            }
            if (Main.rand.NextFloat() <= .05f)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WandOfFire"));
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
    }
}