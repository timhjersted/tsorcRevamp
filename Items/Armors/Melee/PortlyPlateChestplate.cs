using Humanizer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class PortlyPlateChestplate : ModItem
    {
        public static float DamageIncrease = 12f;
        public static int LifeRegen1 = 2;
        public static float LifeThreshold = 25f;
        public static int LifeRegen2 = 3;
        public static int BaseDamage = 45;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageIncrease, LifeRegen1, LifeThreshold, LifeRegen2);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += DamageIncrease / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PortlyPlateHelmet>() && legs.type == ModContent.ItemType<PortlyPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lifeRegen += LifeRegen1;
            player.noKnockback = true;
            player.GetModPlayer<tsorcRevampPlayer>().PortlyPlateArmor = true;
            if (player.ownedProjectileCounts[ModContent.ProjectileType<PortlyPlateRollHitbox>()] == 0)
            {
                int projectile = Projectile.NewProjectile(Projectile.GetSource_None(), player.Center, Vector2.Zero, ModContent.ProjectileType<PortlyPlateRollHitbox>(), BaseDamage, 11.5f, player.whoAmI);
                Main.projectile[projectile].originalDamage = BaseDamage;
            }
            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.lifeRegen += LifeRegen2;
                Dust.NewDust(player.Center, 10, 10, DustID.AmberBolt);
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "RollDmg", Language.GetTextValue("Mods.tsorcRevamp.Items.PortlyPlateChestplate.SetBonus").FormatWith((int)Main.LocalPlayer.GetTotalDamage(DamageClass.Melee).ApplyTo(BaseDamage), Main.LocalPlayer.GetTotalCritChance(DamageClass.Melee))));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GladiatorBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
