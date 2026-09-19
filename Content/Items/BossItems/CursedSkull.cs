using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.BossItems
{
    class CursedSkull : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DeathBringer));
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
        }

        public override bool? UseItem(Player player)
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.CursedSkull.Summon"), 175, 75, 255);
            NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.position.X - 1070, (int)player.position.Y - 150, NPCID.SkeletronHead, 0);

            return true;

        }
        public override bool CanUseItem(Player player) //this has to go in CanUseItem. If used in UseItem, it prints text  every frame the item is "in use", leading to text spam
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (!Main.dayTime && player.ZoneJungle && player.ZoneRockLayerHeight) //to ensure it is used in the right place
                {
                    return true;
                }
                else
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.CursedSkull.Wrong"), 220, 180, 180);
                    return false;
                }
            }

            else //if not adventure mode
            {
                if (!Main.dayTime)
                {
                    return true;
                }
                else
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.CursedSkull.WrongTime"), 220, 180, 180);
                    return false;
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
