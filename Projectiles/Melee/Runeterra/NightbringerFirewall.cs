using Terraria;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using Terraria.Audio;
using ReLogic.Utilities;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    public class NightbringerFirewall: ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 250;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Nightbringer.WindwallDuration * 60;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }
        SlotId SoundSlotID;
        bool soundPaused;
        bool playedSound = false;
        ActiveSound FirewallSound;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 unitVectorTowardsMouse = player.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/FirewallCast") with { Volume = 1f });
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.CritChance *= 2;
            if (!playedSound)
            {
                playedSound = true;
                SoundSlotID = SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/FirewallAmbient") with { Volume = 1f }); //can give funny pitch hehe
            }
            if (playedSound)
            {
                if (FirewallSound == null)
                {
                    SoundEngine.TryGetActiveSound(SoundSlotID, out FirewallSound);
                }
                else
                {
                    if (SoundEngine.AreSoundsPaused && !soundPaused)
                    {
                        FirewallSound.Pause();
                        soundPaused = true;
                    }
                    else if (!SoundEngine.AreSoundsPaused && soundPaused)
                    {
                        FirewallSound.Resume();
                        soundPaused = false;
                    }
                    FirewallSound.Position = Projectile.Center;
                }
            }
            if (Projectile.timeLeft == Nightbringer.WindwallDuration * 60 - 15)
            {
                Projectile.velocity = Vector2.Zero;
            }
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (i != Projectile.whoAmI && other.active && !other.friendly && Projectile.Hitbox.Intersects(other.Hitbox) && UsefulFunctions.IsProjectileSafeToFuckWith(i))
                {
                    Dust.NewDust(other.position, other.width * 2, other.height * 2, DustID.Torch);
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/FirewallHit") with { Volume = 1f });
                    other.Kill();
                }
            }
            float frameSpeed = 5f;

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
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 5f);
        }
        public override void OnKill(int timeLeft)
        {
            if (FirewallSound == null)
            {
                SoundEngine.TryGetActiveSound(SoundSlotID, out FirewallSound);
                if (FirewallSound != null)
                {
                    FirewallSound.Stop();
                }
            }
            else
            {
                FirewallSound.Stop();
            }
        }
    }
}