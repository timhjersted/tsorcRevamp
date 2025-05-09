using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Items.Debug
{
    [AutoloadEquip(EquipType.Legs)]
    public class Linklight2sPants : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/RedHerosPants";
        public static float MoveSpeed = 30f;
        public static int MinionSlots = 10;
        public static float MaxStamina = 30f;
        public static float StaminaRegen = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MinionSlots, MaxStamina, StaminaRegen);
        public static int ConsSoulChanceAmp = 50;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 25;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<Linklight2sHat>() && body.type == ModContent.ItemType<Linklight2sShirt>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void UpdateEquip(Player player)
        {
            if (!Terraria.ModLoader.ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            player.moveSpeed += MoveSpeed / 100f;
            if (!ModContent.GetInstance<tsorcRevampConfig>().DisableDragoonGreavesDoubleJump)
            {
                player.GetJumpState(ExtraJump.UnicornMount).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
            }
            player.jumpBoost = true;
            player.maxMinions += MinionSlots;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;

            int playerX = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int playerY = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(playerX, playerY, 0.75f, 0.75f, 1.5f);

            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 10;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += ConsSoulChanceAmp / 5;
            player.GetModPlayer<tsorcRevampPlayer>().SoulSickle = true;

            Lighting.AddLight(player.Center, 0.4f, 0.7f, 0.6f);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            
            recipe.AddIngredient(ItemID.CopperGreaves, 1);
            recipe.AddCondition(tsorcRevampWorld.DebugModeEnabled);

            recipe.Register();
        }
    }
}

