using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems
{
    public class PottedPlanteraBulb : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Summons Plantera");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 32;
            Item.maxStack = 1;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Lime;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ZoneJungle)
            {
                return !NPC.AnyNPCs(NPCID.Plantera);
            }
            return false;
        }

        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, -1, -1, null, player.whoAmI, 262f);
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleSpores, 9);
            recipe.AddIngredient(ItemID.HallowedBar, 1);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
