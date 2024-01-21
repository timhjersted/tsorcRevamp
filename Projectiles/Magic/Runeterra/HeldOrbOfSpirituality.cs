using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class HeldOrbOfSpirituality : HeldRuneterraOrb
    {
        public override int Width => 62;
        public override int Height => 106;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/";
        public override int NotFilledDustID => DustID.VenomStaff;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 3;
        public override Color NotFilledColor => Color.Pink;
        public override int OrbItemType => ModContent.ItemType<OrbOfSpirituality>();
        public override int ThrownOrbType => ModContent.ProjectileType<ThrownOrbOfSpirituality>();
        public override int FrameSpeed => 7;
        public override int SoundCooldown => 660;
        public override int DistanceToPlayerX => 36;
        public override int DistanceToPlayerY => 42;

        bool playedSound2 = false;
        public override void PlaySound(Player player)
        {
            if (!playedSound && !playedSound2 && Main.rand.NextBool(2000) && !player.HasBuff(ModContent.BuffType<InCombat>()))
            {
                playedSound = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle(SoundPath + "OrbAmbient1", SoundType.Ambient) with { Volume = OrbOfDeception.OrbSoundVolume }); //can give funny pitch hehe
            }
            else if (!playedSound && !playedSound2 && Main.rand.NextBool(2000) && !player.HasBuff(ModContent.BuffType<InCombat>()))
            {
                playedSound2 = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle(SoundPath + "OrbAmbient2", SoundType.Ambient) with { Volume = OrbOfDeception.OrbSoundVolume }); //can give funny pitch hehe
            }
            if (playedSound || playedSound2)
            {
                if (OrbSound == null)
                {
                    SoundEngine.TryGetActiveSound(SoundSlotID, out OrbSound);
                }
                else
                {
                    if (SoundEngine.AreSoundsPaused && !soundPaused)
                    {
                        OrbSound.Pause();
                        soundPaused = true;
                    }
                    else if (!SoundEngine.AreSoundsPaused && soundPaused)
                    {
                        OrbSound.Resume();
                        soundPaused = false;
                    }
                    OrbSound.Position = Projectile.Center;
                }
                SoundCD++;
                if (playedSound && SoundCD >= SoundCooldown)
                {
                    playedSound = false;
                    SoundCD = 0;
                    playedSound = false;
                }
                else if (playedSound2 && SoundCD >= 300)
                {
                    playedSound2 = false;
                    SoundCD = 0;
                    playedSound2 = false;
                }
            }
        }
    }
}