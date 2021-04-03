using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ResentfulSeedling : ModNPC // Renewable source of wood
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Resentful Seedling");
            Main.npcFrameCount[npc.type] = 7;
        }
    
        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.CorruptBunny);
            aiType = NPCID.CorruptBunny;
            npc.width = 10;
            npc.height = 16;
            npc.HitSound = SoundID.NPCHit33;
            npc.DeathSound = SoundID.NPCDeath29;
            npc.knockBackResist = .75f;
            npc.damage = 10;
            npc.lifeMax = 14;
            npc.defense = 6;
            animationType = NPCID.CorruptBunny;
            npc.value = 0;
        }
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if ((item.type == ItemID.CopperAxe) || (item.type == ItemID.TinAxe) || (item.type == ItemID.IronAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.SilverAxe) || (item.type == ItemID.TungstenAxe) || (item.type == ItemID.GoldAxe) || (item.type == ItemID.PlatinumAxe)
                /*continued*/|| (item.type == ItemID.WarAxeoftheNight) || (item.type == ItemID.BloodLustCluster) || (item.type == ItemID.MeteorHamaxe) || (item.type == ItemID.MoltenHamaxe) || (item.type == ItemID.CobaltWaraxe) || (item.type == ItemID.CobaltChainsaw) || (item.type == ItemID.PalladiumWaraxe) || (item.type == ItemID.PalladiumChainsaw)
                /*half way ugh*/|| (item.type == ItemID.MythrilWaraxe) || (item.type == ItemID.MythrilChainsaw) || item.type == ItemID.OrichalcumWaraxe || (item.type == ItemID.OrichalcumChainsaw) || (item.type == ItemID.AdamantiteWaraxe) || (item.type == ItemID.AdamantiteChainsaw) || (item.type == ItemID.TitaniumWaraxe)
                /*regret*/|| (item.type == ItemID.TitaniumChainsaw) || (item.type == ItemID.PickaxeAxe) || (item.type == ItemID.SawtoothShark) || (item.type == ItemID.Drax) || (item.type == ItemID.ChlorophyteGreataxe) || (item.type == ItemID.ChlorophyteChainsaw) || (item.type == ItemID.ButchersChainsaw)
                /*Do ittttttttt! Kill meeeee! Aghhh agh aghh!*/|| (item.type == ItemID.TheAxe) || (item.type == ItemID.Picksaw) || (item.type == ItemID.ShroomiteDiggingClaw) || (item.type == ItemID.SpectreHamaxe) || (item.type == ItemID.SolarFlareAxe) || (item.type == ItemID.NebulaAxe) || (item.type == ItemID.StardustAxe)
                 || (item.type == ItemID.VortexAxe) || item.type == mod.ItemType("AdamantitePoleWarAxe") || item.type == mod.ItemType("AdamantiteWarAxe") || item.type == mod.ItemType("AncientFireAxe") || item.type == mod.ItemType("CobaltPoleWarAxe") || item.type == mod.ItemType("CobaltWarAxe")
                /*top 10 biggest mistakes of my life*/|| item.type == mod.ItemType("DunlendingAxe") || item.type == mod.ItemType("EphemeralThrowingAxe") || item.type == mod.ItemType("FieryPoleWarAxe") || item.type == mod.ItemType("FieryWarAxe") || item.type == mod.ItemType("HallowedGreatPoleAxe")
                /*spent more time making this list than the NPC iteself*/|| item.type == mod.ItemType("MythrilPoleWarAxe") || item.type == mod.ItemType("MythrilWarAxe") || item.type == mod.ItemType("OldAxe") || item.type == mod.ItemType("OldDoubleAxe") || item.type == mod.ItemType("OldHalberd")
                || item.type == mod.ItemType("ReforgedOldAxe") || item.type == mod.ItemType("ReforgedOldDoubleAxe") || (item.type == mod.ItemType("ReforgedOldHalberd")) || (item.type == mod.ItemType("ForgottenAxe")) || (item.type == mod.ItemType("ForgottenGreatAxe")))

            {
                damage *= 2; //I never want to see or hear the word "axe" again in my life
                if (damage < 10)
                {
                    damage = 10;
                }
            }
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustType = 7;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-3, 0);
                dust.noGravity = false;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 7, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                }
            }
        }
        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            Item.NewItem(npc.getRect(), ItemID.Wood);

            if (Main.rand.Next(3) == 0) //sometimes drop 2 wood
            {
                Item.NewItem(npc.getRect(), ItemID.Wood);
            }

        }
    }
}
