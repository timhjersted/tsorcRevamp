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
            player.GetDamage(DamageClass.Generic) *= modPlayer.BearerOfTheCurse ? 1.4f : 1.2f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= modPlayer.BearerOfTheCurse ? 1.6f : 1.3f;
            modPlayer.PowerWithin = true;
        }
    }
}