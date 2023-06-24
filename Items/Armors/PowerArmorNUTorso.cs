using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class PowerArmorNUTorso : ModItem
    {
        public static int AmmoChance = 20; //changing this number has no effect
        public static float ManaCost = 10f;
        public static int MaxMana = 80;
        public static float AtkSpeed = 20f;
        public static int LifeRegen = 8;
        public static float StaminaRegen = 15f;
        public static float MaxStaminaPercent = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AmmoChance, ManaCost, MaxMana, AtkSpeed, LifeRegen, StaminaRegen, MaxStaminaPercent);
        public override void SetStaticDefaults()
        {
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
            player.ammoCost80 = true;
            player.manaCost -= ManaCost;
            player.statManaMax2 += MaxMana;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PowerArmorNUHelmet>() && legs.type == ModContent.ItemType<PowerArmorNUGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += AtkSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;

            player.lifeRegen += LifeRegen;

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStaminaPercent / 100f;

            if (player.wet || player.lavaWet)
            {
                player.lifeRegen += LifeRegen;
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
