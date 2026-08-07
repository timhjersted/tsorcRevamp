using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Accessories
{
    public class PhoenixRebirthCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Zombie126;
            LastTickSoundVolume = 2f;
            Main.buffNoSave[Type] = true; //this lets you reconnect to cheese the cooldown in theory but I figure we can just let players do it if they really want to, most won't want to do this
            //better than having the buff stay on your character permanently after the first revive if they don't like it and unequip it?
            Main.persistentBuff[Type] = true;
        }
        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex]++;
        }
    }
}
