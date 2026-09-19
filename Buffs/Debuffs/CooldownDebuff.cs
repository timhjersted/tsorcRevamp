using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public abstract class CooldownDebuff : ModBuff
    {
        ///<summary> 
        ///Whether it should play a sound in it's last tick or not.
        ///Useful for signaling a cooldown wearing off.
        ///Make sure to assign a valid string to LastTickSoundPath in CustomSetStaticDefaults. 
        ///Adjust the volume by changing LastTickSoundVolume in CustomSetStaticDefaults.
        ///Set PlaysVanillaSound to true if you're using a sound from Vanilla and assign it's ID to VanillaSoundID.
        ///</summary>
        public abstract bool PlaysSoundOnLastTick { get; }

        /// <summary>
        /// Set this in an override of LastTickSoundSettings
        /// </summary>
        public string LastTickSoundPath;
        /// <summary>
        /// Set this in an override of LastTickSoundSettings
        /// </summary>
        public bool PlaysVanillaSound = false;
        /// <summary>
        /// Set this in an override of LastTickSoundSettings if you enabled PlaysVanillaSound
        /// </summary>
        public SoundStyle VanillaSoundID;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        /// <summary>
        /// Override this to set Sound type and Volume just before the sound is played
        /// </summary>
        public virtual void LastTickSoundsSettings(out float soundVolume)
        {
            soundVolume = 1f;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] <= 0 && PlaysSoundOnLastTick)
            {
                LastTickSoundsSettings(out float soundVolume);
                if (!LastTickSoundPath.StartsWith("tsorcRevamp/"))
                {
                    LastTickSoundPath = "tsorcRevamp/" + LastTickSoundPath;
                }
                if (!PlaysVanillaSound)
                {
                    SoundEngine.PlaySound(new SoundStyle(LastTickSoundPath) with { Volume = soundVolume });
                }
                else
                {
                    SoundEngine.PlaySound(VanillaSoundID with { Volume = soundVolume });
                }
            }
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.buffTime[buffIndex] <= 0 && PlaysSoundOnLastTick)
            {
                LastTickSoundsSettings(out float soundVolume);
                if (!PlaysVanillaSound)
                {
                    SoundEngine.PlaySound(new SoundStyle(LastTickSoundPath) with { Volume = soundVolume }, npc.Center);
                }
                else
                {
                    SoundEngine.PlaySound(VanillaSoundID with { Volume = soundVolume }, npc.Center);
                }
            }
        }
    }
}
