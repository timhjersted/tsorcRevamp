using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    ///<summary>
    ///Sandbox-mode summon for the Vessel of Souls (the Eye of Cthulhu replacement boss). Spawns it above
    ///the player. NOTE: no dedicated sprite or recipe yet — reuses an existing soul-skull item texture so
    ///it loads; flag a bespoke sprite + recipe (or leave debug-only) for a later pass.
    ///</summary>
    class VesselOfSoulsSpawner : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Placeable/SoulSkullItem";

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
            Item.maxStack = 20;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool CanUseItem(Player player)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>());
        }

        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.Center.X, (int)player.Center.Y - (16 * 20), ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>());
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>());
            }
            return true;
        }
    }
}
