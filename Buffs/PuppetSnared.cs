using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    /// <summary>Movement pressure maintained by a live virtual cobweb net. The net refreshes this
    /// in two-tick slices so destroying its final strand releases the player immediately.</summary>
    public class PuppetSnared : ModBuff
    {
        // This debuff intentionally reuses Terraria's Webbed icon; without an explicit texture,
        // tModLoader expects a new Buffs/PuppetSnared.png beside this class.
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Webbed;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 0.55f;
            player.maxRunSpeed *= 0.55f;
            player.runAcceleration *= 0.45f;
            player.jumpSpeedBoost -= 1.5f;
        }
    }
}
