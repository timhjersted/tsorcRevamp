using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class WallTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wall Tome");
            Tooltip.SetDefault("A lost tome that is consumed on use\n" +
                               "Casts Wall on the player, raising defense by 25 for 25 seconds" +
                               "\nDoes not stack with Fog, Barrier or Shield spells" +
                               "\nReduces damage dealt by 20% and movement speed by 15%" +
                               "\nCannot be used again for 60 seconds after wearing off");
        }

        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 50;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.value = 8000;
            Item.consumable = true;

        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronskinPotion);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.Wall>(), 1500, false);
            player.AddBuff(ModContent.BuffType<Buffs.ShieldCooldown>(), 5100); //85 seconds (60 seconds downtime)
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
            {
                return false;
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>()))
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