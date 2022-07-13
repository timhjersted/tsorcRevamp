using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Set bonus grants +30% Melee damage, +30% Melee Speed, +30 Rapid Life Regen, +30% Melee Crit" +
                "\n+12 special abilities of the Ninja." +
                "\nThese include: Firewalk, No fall damage, No knockback, rapid pick speed, waterwalk," +
                "\nreduced potion cooldown, double jump, jump boost, +30 % movement speed," +
                "\narchery, immunity to fire, and night vision." +
                "\nDefense is capped at 40\nDamage reduction is converted into movement speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShadowNinjaTop>() && legs.type == ModContent.ItemType<ShadowNinjaBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.3f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
            player.GetCritChance(DamageClass.Melee) += 30;
            player.fireWalk = true;
            player.noFallDmg = true;
            player.noKnockback = true;
            player.pickSpeed += 0.2f;
            player.waterWalk = true;
            player.pStone = true;
            player.hasJumpOption_Cloud = true;
            player.jumpBoost = true;
            player.moveSpeed += 0.3f;
            player.archery = true;
            player.fireWalk = true;
            player.nightVision = true;
            if (player.statDefense >= 40)
            {
                player.statDefense = 40;
            }
            player.lifeRegen += 30;
            player.moveSpeed += player.endurance;
            player.endurance = 0f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlackBeltHairStyle>());
            recipe.AddIngredient(ItemID.SoulofFright);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
