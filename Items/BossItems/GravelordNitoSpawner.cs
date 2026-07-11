using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    ///<summary>
    ///Sandbox-mode summon for Gravelord Nito. The adventure-mode encounter is a scripted event (built
    ///separately); this item exists purely so sandbox players without that event can still fight him.
    ///NOTE: intentionally has no AddRecipes — no thematic crafting materials were specified. Flag for
    ///a future recipe decision (dungeon-tier materials would be the natural fit) or leave unbuyable/
    ///debug-only if that's preferred.
    ///</summary>
    class GravelordNitoSpawner : ModItem
    {
        // The dedicated item sprite was never added; reuse the existing soul-skull asset so the
        // item can load and remain visually appropriate until bespoke Nito summon art exists.
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
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool CanUseItem(Player player)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.GravelordNito.GravelordNito>());
        }

        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.Center.X, (int)player.Center.Y - (16 * 12), ModContent.NPCType<NPCs.Bosses.GravelordNito.GravelordNito>());
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, ModContent.NPCType<NPCs.Bosses.GravelordNito.GravelordNito>());
            }
            return true;
        }
    }
}
