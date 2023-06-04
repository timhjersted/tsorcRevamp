using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class PowerWithin : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = true;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            player.GetDamage(DamageClass.Generic) *= modPlayer.BearerOfTheCurse ? (1f + (2f * Items.PowerWithin.DamageIncrease / 100f)) : (1f + Items.PowerWithin.DamageIncrease / 100f);
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= modPlayer.BearerOfTheCurse ? (1f + (2f * Items.PowerWithin.StaminaRegen / 100f)) : (1f + Items.PowerWithin.StaminaRegen / 100f);
            modPlayer.PowerWithin = true;
        }
    }
}