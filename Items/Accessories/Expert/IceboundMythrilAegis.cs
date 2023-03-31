using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.Shield)]
    public class IceboundMythrilAegis: ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Inherits Mythril Bulwark, Ankh Shield and Frozen Shield effects" +
                               "\nIncludes immunity to Chilled and Stoned" +
                               "\nSlows down your dogerolling slightly" +
                               "\nRolling through an enemy also grants the Ice Barrier buff temporarily" +
                               "\nRolling through an enemy may inflict some debuffs this item grants immunity to");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.defense = 12;
            Item.expert = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Expert;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AnkhShield);
            recipe.AddIngredient(ItemID.FrozenShield);
            recipe.AddIngredient(ModContent.ItemType<MythrilBulwark>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().IceboundMythrilAegis = true;

            //Ankh Shield inheritance
            player.noKnockback = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.BrokenArmor] = true;
            player.buffImmune[BuffID.Burning] = true;
            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Cursed] = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.Stoned] = true;
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Silenced] = true;
            player.buffImmune[BuffID.Slow] = true;
            player.buffImmune[BuffID.Weak] = true;
            player.buffImmune[BuffID.Chilled] = true;

            //Paladins Shield effect
            if ((float)player.statLife > ((float)player.statLifeMax2 * 0.25f))
            {
                player.hasPaladinShield = true;
                if (player.whoAmI != Main.myPlayer && player.miscCounter % 10 == 0)
                {
                    int myPlayer = Main.myPlayer;
                    if (Main.player[myPlayer].team == player.team && player.team != 0)
                    {
                        float num4 = player.position.X - Main.player[myPlayer].position.X;
                        float num2 = player.position.Y - Main.player[myPlayer].position.Y;
                        if ((float)Math.Sqrt(num4 * num4 + num2 * num2) < 800f)
                        {
                            Main.player[myPlayer].AddBuff(43, 20);
                        }
                    }
                }
            }

            //Frozen Turtle Shell effect
            if ((float)player.statLife <= ((float)player.statLifeMax2 * 0.5f))
            {
                player.AddBuff(BuffID.IceBarrier, 1);
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) player.AddBuff(BuffID.WeaponImbueFire, 60, false);
        }
    }
}

