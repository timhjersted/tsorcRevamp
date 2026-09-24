using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage
{
    public class OwlRing : ModItem
    {
        // "Swing time" gate for the damage/crit/poise bonuses below - item.useAnimation, matching the
        // codebase's existing swing-time convention (see SteraksGage, puppet-swing-tuning skill).
        public const int MinSwingTimeForBonus = 32;
        public static float Crit = 15f;
        public static float DamageBonus = 15f;
        public static float PoiseDamageBonus = 15f;
        public static int MinionSlotBonus = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MinSwingTimeForBonus, DamageBonus, Crit, PoiseDamageBonus, MinionSlotBonus);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.buffImmune[BuffID.Darkness] = true;
            player.maxMinions += MinionSlotBonus;

            // This flag also gates the poise-damage bonus in GlobalNPC.ApplyOwlRingPoiseBonus and the
            // stagger-immunity hyper armor in Buffs/Debuffs/Stagger.Apply, since both are computed at
            // the hit's location rather than here.
            player.GetModPlayer<tsorcRevampPlayer>().OwlRingEquipped = true;

            if (player.HeldItem.useAnimation >= MinSwingTimeForBonus)
            {
                player.GetDamage(DamageClass.Generic) += DamageBonus / 100f;
                player.GetCritChance(DamageClass.Generic) += Crit;
            }
        }

    }
}

