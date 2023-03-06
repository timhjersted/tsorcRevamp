using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class BoltChainImmunity : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // Description.SetDefault("Can not be struck by a bolt chain lightning again until this expires");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {

        }
    }
}