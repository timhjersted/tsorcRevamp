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
            player.GetDamage(DamageClass.Generic) *= 1f + Items.Tools.PowerWithin.DamageIncrease / 100f;
            // Regen goes on staminaResourceGainMult, additively, NOT on staminaResourceRegenRate. regenRate is a
            // separate multiplicative channel (gain = gainMult x regenRate), so anything living there sits outside
            // the additive gear pool and escapes every balance lever applied to it.
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += Items.Tools.PowerWithin.StaminaRegen / 100f;
            modPlayer.PowerWithin = true;
        }
    }
}