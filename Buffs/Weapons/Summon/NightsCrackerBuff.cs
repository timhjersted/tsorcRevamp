using Humanizer;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Weapons.Summon
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
            AttackSpeed = player.GetModPlayer<tsorcRevampPlayer>().NightsCrackerStacks * NightsCracker.MinSummonTagAttackSpeed;
            player.GetAttackSpeed(DamageClass.Summon) += AttackSpeed / 100;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip += LangUtils.GetTextValue("CommonItemTooltip.Summon.AtkSpeed").FormatWith((int)AttackSpeed);
        }
    }
}
