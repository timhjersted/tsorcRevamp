using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public abstract class HeldRuneterraOrb : ModProjectile
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract string SoundPath { get; }
        public abstract int NotFilledDustID { get; }
        public abstract int FilledDustID { get; }
        public abstract int Tier { get; }

        public Color FilledColor;
        public abstract Color NotFilledColor { get; }
        public abstract int OrbItemType { get; }
        public abstract int ThrownOrbType { get; }
        public abstract int FrameSpeed { get; }
        public abstract int SoundCooldown { get; }
        public abstract int DistanceToPlayerX { get; }
        public abstract int DistanceToPlayerY { get; }
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
            Projectile.width = Width; // The width of your projectile
            Projectile.height = Height; // The height of your projectile
            Projectile.friendly = true; // Deals damage to enemies
            Projectile.penetrate = -1; // Infinite pierce
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            switch (Tier)
            {
                case 1:
                    {
                        FilledColor = OrbOfDeception.FilledColor;
                        break;
                    }
                case 2:
                    {
                        FilledColor = OrbOfFlame.FilledColor;
                        break;
                    }
                case 3:
                    {
                        FilledColor = OrbOfSpirituality.FilledColor;
                        break;
                    }
            }
        }

        public SlotId SoundSlotID;
        public bool soundPaused;
        public bool playedSound = false;
        public ActiveSound OrbSound;
        public int SoundCD = 0;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            DecideToKillOrb(player); //make it kill the held orb if one is thrown out, so it's as if there's only one orb

            if (player.HeldItem.type == OrbItemType) //keeping the projectile alive while it's item is being held
            {
                Projectile.timeLeft = 600;
            }

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, player.direction * -1.9f); //setting custom arm frame

            PlaySound(player); //decide whether to play the ambient sound or not

            Vector2 VelocityToPlayerHand = new Vector2(player.Center.X + (DistanceToPlayerX * player.direction), player.Center.Y - DistanceToPlayerY);
            Projectile.Center = VelocityToPlayerHand;

            //Projectile.velocity = Projectile.Center.DirectionTo(VelocityToPlayerHand) * (VelocityToPlayerHand.Distance(Projectile.Center)); why doesn't this work wat

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= FrameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().EssenceThief > 8)
            {
                Dust.NewDust(Projectile.TopLeft, Width, Height, FilledDustID, 0, 0, 150, default, 0.5f);
                Lighting.AddLight(Projectile.Center, FilledColor.ToVector3() * 2f);
            }
            else
            {
                Dust.NewDust(Projectile.TopLeft, Width, Height, NotFilledDustID, 0, 0, 150, default, 0.5f);
                Lighting.AddLight(Projectile.Center, NotFilledColor.ToVector3() * 2f);
            }
        }
        public virtual void DecideToKillOrb(Player player)
        {
            if (player.ownedProjectileCounts[ThrownOrbType] > 0 || player.dead)
            {
                Projectile.Kill();
            }
        }
        public virtual void PlaySound(Player player)
        {
            if (!playedSound && Main.rand.NextBool(2000) && !player.HasBuff(ModContent.BuffType<InCombat>())) // ambient orb sound
            {
                playedSound = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle(SoundPath + "OrbAmbient", SoundType.Ambient) with { Volume = OrbOfDeception.OrbSoundVolume }); //can give funny pitch hehe
            }
            if (playedSound)
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
                if (SoundCD >= SoundCooldown)
                {
                    playedSound = false;
                    SoundCD = 0;
                }
            }
        }
        public override void OnKill(int timeLeft)
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
        public override bool? CanDamage()
        {
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().EssenceThief > 8)
            {
                lightColor = FilledColor;
                return base.PreDraw(ref lightColor);
            }
            return base.PreDraw(ref lightColor);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
    }
}