using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    class CrystalShield : ModBuff
    {
        public int defense;
        public int damage;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Shield");
            // Description.SetDefault("Defense increased");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += (int)player.GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage;
            defense = (int)player.GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage;
            damage = (int)(25 - (int)(player.GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage) * 1.67f);
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.endurance -= (25f - (player.GetModPlayer<tsorcRevampPlayer>().CrystalDefenseDamage) * 1.67f) / 100f;
            }
        }
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            tip = $"Defense increased by {defense}, damage dealt increased by {damage}";
        }
    }
}
