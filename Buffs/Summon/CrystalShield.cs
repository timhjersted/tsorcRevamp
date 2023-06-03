using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    public class CrystalShield : ModBuff
    {
        public int Defense;

        public override LocalizedText Description => base.Description.WithFormatArgs(Defense);

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            player.statDefense += (int)modPlayer.CrystalNunchakuDefenseDamage;

            Defense = (int)modPlayer.CrystalNunchakuDefenseDamage;

            /*if (modPlayer.BearerOfTheCurse)
            {
                player.endurance -= (25f - modPlayer.CrystalNunchakuDefenseDamage * 1.67f) / 100f;
            }*/
        }
    }
}
