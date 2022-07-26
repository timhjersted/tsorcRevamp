using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask2 : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Armors/Melee/ShadowNinjaMask";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Ninja Mask II");
            Tooltip.SetDefault("Set bonus grants +30% to all damage, 100 mana, +30% Melee Speed, +30 Rapid Life Regen, +30% Crit" +
                "\n+12 special abilities of the Ninja." +
                "\nThese include: Firewalk, No fall damage, No knockback, rapid pick speed, waterwalk," +
                "\nreduced potion cooldown, double jump, jump boost, +30 % movement speed," +
                "\narchery, immunity to fire, and night vision." +
                "\nDefense is capped at 60\nDamage reduction is converted into movement speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Items.Armors.Melee.ShadowNinjaTop>() && legs.type == ModContent.ItemType<Items.Armors.Melee.ShadowNinjaBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
            player.GetCritChance(DamageClass.Generic) += 30;
            player.fireWalk = true;
            player.noFallDmg = true;
            player.noKnockback = true;
            player.pickSpeed += 0.2f;
            player.waterWalk = true;
            player.pStone = true;
            player.hasJumpOption_Basilisk = true;
            player.jumpBoost = true;
            player.moveSpeed += 0.3f;
            player.archery = true;
            player.fireWalk = true;
            player.nightVision = true;
            if(player.statDefense >=60)
            {
                player.statDefense = 60;
            }
            player.lifeRegen += 30;
            player.moveSpeed += player.endurance;
            player.endurance = 0f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Items.Armors.Melee.ShadowNinjaMask>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
