using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class DragoonGreaves : ModItem
    {
        public static float MoveSpeed = 33f;
        public static int MinionSlots = 2;
        public static float MaxStamina = 18f;
        public static float StaminaRegen = 18f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MinionSlots, MaxStamina, StaminaRegen);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            //If the tier 1 head or chestplate are on, they will handle the set bonus.
            //Applying it again here would make it apply twice.
            if (head.type == ModContent.ItemType<DragoonHelmet>() || body.type == ModContent.ItemType<DragoonArmor>())
            {
                return false;
            }

            //If neither the head or legs are on, check if the upgraded versions are. If so, apply the set bonus.
            if (head.type == ModContent.ItemType<DragoonHelmet2>() && body.type == ModContent.ItemType<DragoonArmor2>())
            {
                return true;
            }

            return false;
        }

        public override void UpdateArmorSet(Player player)
        {
            DragoonHelmet.ApplyDragoonSetBonus(player);
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            if (!ModContent.GetInstance<tsorcRevampConfig>().DisableDragoonGreavesDoubleJump)
            {
                player.GetJumpState(ExtraJump.UnicornMount).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
            }
            player.jumpBoost = true;
            player.maxMinions += MinionSlots;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedHerosPants>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofFright, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 26000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

