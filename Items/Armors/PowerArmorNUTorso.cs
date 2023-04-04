using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class PowerArmorNUTorso : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Power Armor NU Torso");
            Tooltip.SetDefault("A powerful armor forged by the god of chaos." +
                "\nIncreases ranged damage by 17% and magic damage by 9%" +
                "\nReduces chance of consuming ammo by 20%, increases max mana by 80 and decreases mana costs by 10%" +
                "\nSet Bonus: +20% attack speed(doubled for melee), +8 life regen/15 in water" +
                "\nIncreases maximum stamina and stamina regen by 15%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.17f;
            player.GetDamage(DamageClass.Magic) += 0.09f;
            player.ammoCost80 = true;
            player.manaCost -= 0.1f;
            player.statManaMax2 += 80;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PowerArmorNUHelmet>() && legs.type == ModContent.ItemType<PowerArmorNUGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.2f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;

            player.lifeRegen += 8;

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1.15f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1.15f;

            if (player.wet)
            {
                player.lifeRegen += 7;
            }
            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 39, (player.velocity.X) + (player.direction * 2), player.velocity.Y, 100, Color.SpringGreen, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].noLight = false;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
