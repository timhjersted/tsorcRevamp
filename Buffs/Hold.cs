using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Hold : ModBuff
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Held");
            Description.SetDefault("You're being held in position!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.velocity.X = 0;
            player.velocity.Y = 0;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.velocity.X = 0;
            npc.velocity.Y = 0;
        }
    }
}
