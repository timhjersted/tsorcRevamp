using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class CursedSkull : ModItem {
        public override string Texture => "tsorcRevamp/Items/BossItems/BloodySkull";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons Skeletron, the First of the Dead." +
                                "\nYou must use this at the demon altar in the ancient temple ruins" +
                                "\nBut be warned, this battle will not be easy..." +
                                "\nItem is not consumed so you can retry the fight until victory.");

        }
        public override void SetDefaults() {
            item.width = 12;
            item.height = 12;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 5;
            item.useTime = 5;
        }

        public override bool UseItem(Player player)
        {

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText("Skeletron has awoken!", 175, 75, 255);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Skeletron has awoken!"), new Color(175, 75, 255));
            }
            NPC.NewNPC((int)player.position.X - 1070, (int)player.position.Y - 150, NPCID.SkeletronHead, 0);
            
            return true;

        }
        public override bool CanUseItem(Player player) //this has to go in CanUseItem. If used in UseItem, it prints text  every frame the item is "in use", leading to text spam
        {
            if (!Main.dayTime)
            {
                return true;
            }
            else
            {
                Main.NewText("This item can only be used at night...", 220, 180, 180);
                return false;
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Bone, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
