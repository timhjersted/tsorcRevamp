using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class MechanicalSkull : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
        }

        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>());

            return true;

        }
        public override bool CanUseItem(Player player) //this has to go in CanUseItem. If used in UseItem, it prints text  every frame the item is "in use", leading to text spam
        {
            if (Main.dayTime)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.MechanicalSkull.WrongTime"), 220, 180, 180);
                return false;
            }
            else if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (player.ZoneDungeon && player.ZoneRockLayerHeight) //to ensure it is used in the right place)
                {
                    return true;
                }
                else
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.MechanicalSkull.Wrong"), 220, 180, 180);
                    return false;
                }
            }
            else // Night-time sandbox mode
            {
                return true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1);
            recipe.AddIngredient(ItemID.SoulofFright, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
