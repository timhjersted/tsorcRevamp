using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    class ShellHelmet : ModItem
    {
        public static float CritChance = 16f;
        public static float LifeThreshold = 50f;
        public static int AmmoChance = 20;  //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public const float CritCap = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, LifeThreshold, AmmoChance, CritCap);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 7;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShellArmor>() && legs.type == ModContent.ItemType<ShellGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            if (player.statLife <= (player.statLifeMax2 * LifeThreshold / 100f))
            {
                player.GetCritChance(DamageClass.Ranged) += MathF.Min(player.GetWeaponCrit(player.HeldItem), CritCap);

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Black, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NecroHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
