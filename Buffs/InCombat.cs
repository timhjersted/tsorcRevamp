using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class InCombat : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("In Combat");
            // Description.SetDefault("You are in combat.");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
