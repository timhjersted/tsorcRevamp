using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class CrystalShield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            player.statDefense += (int)modPlayer.CrystalNunchakuDefenseDamage;
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip += (int)Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CrystalNunchakuDefenseDamage;
        }
    }
}
