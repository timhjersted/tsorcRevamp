using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    public class OrbOfSpiritualityCharmCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = "Sounds/Runeterra/Magic/OrbOfSpirituality/CharmReady";
            soundVolume = OrbOfDeception.OrbSoundVolume * 2;
        }
    }
}
