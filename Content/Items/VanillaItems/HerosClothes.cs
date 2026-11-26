using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.VanillaItems
{
    class HerosClothes : GlobalItem
    {

        public const float CritChance = 5f;
        public const float MaxStamina = 10f;
        public const float StaminaRegen = 10f;
        public const float MoveSpeed = 10f;
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.HerosHat && body.type == ItemID.HerosShirt && legs.type == ItemID.HerosPants)
            {
                return "Hero's Clothes";
            }
            else return base.IsArmorSet(head, body, legs);
        }

        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "Hero's Clothes")
            {
                player.setBonus = LangUtils.GetTextValue("Items.VanillaItems.HeroSetBonus", CritChance, MaxStamina, StaminaRegen, MoveSpeed);
                player.GetCritChance(DamageClass.Generic) += CritChance;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegen / 100f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
                player.moveSpeed += MoveSpeed / 100f;
            }
        }

        public override void SetDefaults(Item item)
        {

            if (item.type == ItemID.HerosHat)
            {
                item.vanity = false;
                item.defense = 5;
            }
            if (item.type == ItemID.HerosShirt)
            {
                item.vanity = false;
                item.defense = 7;
            }
            if (item.type == ItemID.HerosPants)
            {
                item.vanity = false;
                item.defense = 5;
            }
        }
    }
}
