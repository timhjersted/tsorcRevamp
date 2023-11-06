using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.BossItems
{
    class AaronsProtectionStone : ModItem
    {
        public const float DamageIncrease = 11f;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 26;
            Item.consumable = false;
            Item.defense = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.value = 1000;
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ZoneUnderworldHeight && !NPC.AnyNPCs(NPCID.WallofFlesh);
        }
        public override void UpdateEquip(Player player)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    player.GetDamage(DamageClass.Generic) += DamageIncrease / 100f;
                }
            }
        }

        public override bool? UseItem(Player player)
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.AaronsProtectionStone.Summon"), 175, 75, 255);
            NPC.SpawnWOF(new Vector2(player.position.X - (1070), player.position.Y - 150));
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe1 = CreateRecipe();
            recipe1.AddIngredient(ItemID.StoneBlock, 1);
            recipe1.AddIngredient(ItemID.GuideVoodooDoll, 1);
            recipe1.Register();
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is AaronsProtectionStone)
                {
                    return false;
                }
            }
            return base.CanEquipAccessory(player, slot, modded);
        }
    }
}
