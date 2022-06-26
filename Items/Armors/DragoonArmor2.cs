using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor2 : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/DragoonArmor";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dragoon Armor II");
            Tooltip.SetDefault("A reforged upgrade to the legendary Dragoon Armor.\n" +
                "You are a master of all forces, the protector of Earth, the Hero of the age.\n" +
                "The powers of the Dragoon Cloak are embedded within its blue-plated chest piece.\n" +
                "Set bonus adds +38% to all stats and +6 HP Regen, while Dragoon Cloak effects kick in at 160 HP.");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 50;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.starCloakItem = new Item(ItemID.StarCloak);
            player.GetCritChance(DamageClass.Magic) += 3;
            player.GetDamage(DamageClass.Magic) += .05f;

            if (player.statLife <= 160)
            {
                player.lifeRegen += 8;
                player.statDefense += 12;
                player.manaRegenBuff = true;
                player.GetCritChance(DamageClass.Magic) += 3;
                player.GetDamage(DamageClass.Magic) += .05f;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DragoonArmor").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DragoonCloak").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 10);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 90000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
