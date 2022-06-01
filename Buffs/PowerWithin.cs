using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class PowerWithin : ModBuff
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Power Within");
            Description.SetDefault("Excessive power is eating away at you!");
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = true;
            canBeCleared = false;

        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.GetDamage(DamageClass.Generic) *= 1.4f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.6f;
            }
            else
            {
                player.GetDamage(DamageClass.Generic) *= 1.2f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.3f;
            }

            player.GetModPlayer<tsorcRevampPlayer>().PowerWithin = true;
        }
    }
}