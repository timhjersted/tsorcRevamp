using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.VanillaItems;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class Bolt3Tome : ModItem
    {
        const float dmgIncrease = 1.3f;
        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.height = 10;
            Item.width = 34;
            Item.knockBack = 0.1f;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 15f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 20;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.Bolt3Lightning>();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.wet)
            {
                type = ModContent.ProjectileType<Projectiles.Magic.Bolt4Lightning>();
                damage = (int)(damage * dmgIncrease);
            }
        }
        // Show increased damage on tooltip when the player is wet
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            if (!player.wet)
                return;

            TooltipLine modified = new TooltipLine(Mod, "Damage", ((int)(player.GetWeaponDamage(Item, true) * dmgIncrease)).ToString() + " magic damage");

            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage")
                {
                    tooltips[i] = modified;
                    break;
                }
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<Bolt2Tome>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
