using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    public class BarrierTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Barrier Tome");
            Tooltip.SetDefault("A lost tome for artisans\n" +
                                "Casts Barrier on the user, which adds 20 defense for 20 seconds\n" +
                                "\nDoes not stack with Fog, Wall or Shield spells");

        }

        public override void SetDefaults()
        {
            item.stack = 1;
            item.width = 34;
            item.height = 10;
            item.maxStack = 1;
            item.rare = ItemRarityID.Orange;
            item.magic = true;
            item.noMelee = true;
            item.mana = 150;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.useAnimation = 20;
            item.value = 42000;

        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.Barrier>(), 1200, false);
            //Projectile.NewProjectile(player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), 0f, 0f, mod.ProjectileType("Barrier"), 0, 0f, player.whoAmI, 0f, 0f);
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Wall>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}