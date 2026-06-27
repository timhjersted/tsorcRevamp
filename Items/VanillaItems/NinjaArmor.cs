using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class NinjaArmor : GlobalItem
    {
        public static float CritChance = 4f;
        public static float Damage = 4f;
        public static float MoveSpeed = 8f;
        public static float MaxStamina = 8f;
        public static float StaminaRegen = 8f;

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.NinjaHood)
            {
                item.vanity = false;
                item.defense = 3;
            }
            if (item.type == ItemID.NinjaShirt)
            {
                item.vanity = false;
                item.defense = 4;
            }
            if (item.type == ItemID.NinjaPants)
            {
                item.vanity = false;
                item.defense = 3;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.NinjaHood)
            {
                AddNinjaTooltip("NinjaHood", tooltips, CritChance);
            }
            if (item.type == ItemID.NinjaShirt)
            {
                AddNinjaTooltip("NinjaShirt", tooltips, Damage);
            }
            if (item.type == ItemID.NinjaPants)
            {
                AddNinjaTooltip("NinjaPants", tooltips, MoveSpeed, MaxStamina, StaminaRegen);
            }
        }

        public override void UpdateEquip(Item item, Player player)
        {
            if (item.type == ItemID.NinjaHood)
            {
                player.GetCritChance(DamageClass.Melee) += CritChance;
                player.GetCritChance(DamageClass.Ranged) += CritChance;
            }
            if (item.type == ItemID.NinjaShirt)
            {
                player.GetDamage(DamageClass.Melee) += Damage / 100f;
                player.GetDamage(DamageClass.Ranged) += Damage / 100f;
            }
            if (item.type == ItemID.NinjaPants)
            {
                player.moveSpeed += MoveSpeed / 100f;
            }
        }

        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.NinjaHood && body.type == ItemID.NinjaShirt && legs.type == ItemID.NinjaPants)
            {
                return "Ninja Armor";
            }

            return base.IsArmorSet(head, body, legs);
        }

        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "Ninja Armor")
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
            }
        }

        private void AddNinjaTooltip(string key, List<TooltipLine> tooltips, params object[] args)
        {
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, key, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems." + key).FormatWith(args)));
            }
        }
    }
}
