using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class ElixirCooldown : ModBuff
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Elixir Cooldown");
            // Description.SetDefault("You cannot drink Holy War Elixirs!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true; //prevents nurse clearing
        }
    }
}
