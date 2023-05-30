using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    public class NightsCrackerBuff : ModBuff
    {
        public float AttackSpeed;

        public override LocalizedText Description => base.Description.WithFormatArgs(AttackSpeed);

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            AttackSpeed = player.GetModPlayer<tsorcRevampPlayer>().NightsCrackerStacks * 6f;
            player.GetAttackSpeed(DamageClass.Summon) += AttackSpeed / 100;
        }
    }
}
