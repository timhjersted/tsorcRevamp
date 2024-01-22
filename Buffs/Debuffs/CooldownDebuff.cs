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
        ///Set PlaysVanillaSound to true if you're using a sound from Vanilla and assign it'S ID to VanillaSoundID.
        ///</summary>
        public abstract bool PlaysSoundOnLastTick { get; }
        ///<summary> 
        ///Assign the path to the Sound played on the last tick to this.
        ///Already includes "tsorcRevamp/Sounds/"
        ///</summary>
        public string LastTickSoundPath;
        public bool PlaysVanillaSound = false;
        public SoundStyle VanillaSoundID;
        public float LastTickSoundVolume = 1f;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            CustomSetStaticDefaults();
        }
        ///<summary> 
        ///Use this to do anything you'd usually do in SetStaticDefaults, as to not overwrite the standard SetStaticDefaults function.
        ///</summary>
        public virtual void CustomSetStaticDefaults()
        { }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == 1 && PlaysSoundOnLastTick)
            {
                if (!PlaysVanillaSound)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/" + LastTickSoundPath) with { Volume = LastTickSoundVolume });
                }
                else
                {
                    SoundEngine.PlaySound(VanillaSoundID with { Volume = LastTickSoundVolume });
                }
            }
            PlayerCustomUpdate(player, ref buffIndex);
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.buffTime[buffIndex] == 1 && PlaysSoundOnLastTick)
            {
                if (!PlaysVanillaSound)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/" + LastTickSoundPath) with { Volume = LastTickSoundVolume }, npc.Center);
                }
                else
                {
                    SoundEngine.PlaySound(VanillaSoundID with { Volume = LastTickSoundVolume }, npc.Center);
                }
            }
            NPCCustomUpdate(npc, ref buffIndex);
        }
        ///<summary> 
        ///Use this to do anything you'd usually do in Update, as to not overwrite the standard update function.
        ///</summary>         
        ///<param name="player">The player to update this buff on.</param>
        ///<param name="buffIndex">The index in player.buffType of this buff. For use with Player.DelBuff(int).</param>
        public virtual void PlayerCustomUpdate(Player player, ref int buffIndex)
        { }
        ///<summary> 
        ///Use this to do anything you'd usually do in Update, as to not overwrite the standard update function.
        ///</summary>         
        ///<param name="npc">The npc to update this buff on.</param>
        ///<param name="buffIndex">The index in npc.buffType of this buff. For use with NPC.DelBuff(int).</param>
        public virtual void NPCCustomUpdate(NPC npc, ref int buffIndex)
        { }
    }
}
