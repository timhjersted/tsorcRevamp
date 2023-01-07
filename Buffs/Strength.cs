using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Strength : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Strength");
            Description.SetDefault("You feel as if you could break the world in two, with your bare hands...");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 15;
            player.GetDamage(DamageClass.Generic) += 0.1f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
        }
    }
}
