using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Items.BossItems
{
    [Autoload(false)]
    class SoulOfCinderSpawner : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.rare = ItemRarityID.Expert;
        }

        public override bool CanUseItem(Player player)
        {
            bool CanUse = false;
            if (!NPC.AnyNPCs(ModContent.NPCType<SoulOfCinder>()))
            {
                CanUse = true;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                if (!UsefulFunctions.IsPointWithinEllipse(player.Center, SoulOfCinder.ARENA_LOCATION_ADVENTURE, SoulOfCinder.ARENA_WIDTH, SoulOfCinder.ARENA_HEIGHT))
                {
                    Main.NewText("This item must be used within the Tomb of Gwyn.", Color.Firebrick);
                    CanUse = false;
                }
            }
            return CanUse;
        }


        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.Center.X, (int)player.Center.Y - (16 * 12), ModContent.NPCType<SoulOfCinder>());
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, ModContent.NPCType<SoulOfCinder>());
            }
            return true;
        }
    }
}
