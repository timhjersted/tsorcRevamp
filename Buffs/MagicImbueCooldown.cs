using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MagicImbueCooldown : ModBuff
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Imbue Cooldown");
            Description.SetDefault("You cannot use any magical weapon imbues!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
