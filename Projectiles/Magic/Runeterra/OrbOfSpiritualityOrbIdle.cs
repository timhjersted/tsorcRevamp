using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using Terraria.DataStructures;
using System.Collections.Generic;
using ReLogic.Utilities;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class OrbOfSpiritualityOrbIdle : ModProjectile
    {

		public override void SetStaticDefaults()
		{
			// These lines facilitate the trail drawing
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
			ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = 8;
        }

		public override void SetDefaults()
		{
			Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
            Projectile.aiStyle = -1;
			Projectile.width = 50; // The width of your projectile
			Projectile.height = 50; // The height of your projectile
			Projectile.friendly = true; // Deals damage to enemies
			Projectile.penetrate = -1; // Infinite pierce
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = false;
		}
        SlotId SoundSlotID;
        bool soundPaused;
        bool playedSound1 = false;
        bool playedSound2 = false;
        ActiveSound OrbSound;
        public int SoundCD = 0;
        public override void AI()
		{
			Player player = Main.player[Projectile.owner];
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, player.direction * -1.9f);
            if (!playedSound1 && !playedSound2 && Main.rand.NextBool(2000) && !player.HasBuff(ModContent.BuffType<InCombat>()))
            {
                playedSound1 = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/OrbAmbient1", SoundType.Ambient) with { Volume = OrbOfDeception.OrbSoundVolume }, player.Center); //can give funny pitch hehe
            } else if (!playedSound1 && !playedSound2 && Main.rand.NextBool(2000) && !player.HasBuff(ModContent.BuffType<InCombat>()))
            {
                playedSound2 = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/OrbAmbient2", SoundType.Ambient) with { Volume = OrbOfDeception.OrbSoundVolume }, player.Center); //can give funny pitch hehe
            }
            if (playedSound1 || playedSound2)
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
                if (playedSound1 && SoundCD >= 660)
                {
                    playedSound1 = false;
                    SoundCD = 0;
                    playedSound1 = false;
                }
                else if (playedSound2 && SoundCD >= 300)
                {
                    playedSound2 = false;
                    SoundCD = 0;
                    playedSound2 = false;
                }
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<OrbOfSpiritualityOrb>()] > 0 || player.dead)
            {
                Projectile.Kill();
            }
            if (player.direction == 1) 
            {
                Projectile.Center = new Vector2(player.Center.X + player.width + 10, player.Center.Y - 70);
            } else
            {
                Projectile.Center = new Vector2(player.Center.X - player.width - 24, player.Center.Y - 70);
            }
            int frameSpeed = 7;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Pink.ToVector3() * 2f);
            if (player.GetModPlayer<tsorcRevampPlayer>().EssenceThief > 8)
            {
                Dust.NewDust(Projectile.Center, 2, 2, DustID.PoisonStaff, 0, 0, 150, default, 0.5f);
                Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3() * 2f);
            } else
            {
                Dust.NewDust(Projectile.Center, 2, 2, DustID.VenomStaff, 0, 0, 150, default, 0.5f);
                Lighting.AddLight(Projectile.Center, OrbOfSpirituality.FilledColor.ToVector3() * 2f);
            }
        }
        public override void Kill(int timeLeft)
        {
            if (OrbSound == null)
            {
                SoundEngine.TryGetActiveSound(SoundSlotID, out OrbSound);
                if (OrbSound != null)
                {
                    OrbSound.Stop();
                }
            }
            else
            {
                OrbSound.Stop();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().EssenceThief > 8)
            {
                lightColor = OrbOfSpirituality.FilledColor;
            }
            return base.PreDraw(ref lightColor);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
}