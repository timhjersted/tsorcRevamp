using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
    public class NightbringerTornado : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 66;
            Projectile.aiStyle = ProjAIStyleID.TwilightLance;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.damage = (int)(player.GetWeaponDamage(player.HeldItem) * (1.5f + player.GetTotalAttackSpeed(DamageClass.Melee))); //scales with attack speed since higher melee speed increases the travelling speed, therefore giving it less time to get hits off
        }
        SlotId SoundSlotID;
        SoundStyle TornadoSoundStyle = SoundID.DD2_BookStaffTwisterLoop;
        bool soundPaused;
        bool playedSound = false;
        ActiveSound TornadoSound;

        public override void AI()
        {
            if (!playedSound)
            {
                playedSound = true;
                SoundSlotID = SoundEngine.PlaySound(SoundID.DD2_BookStaffTwisterLoop, Projectile.Center); //can give funny pitch hehe
                if (TornadoSound == null)
                {
                    SoundEngine.TryGetActiveSound(SoundSlotID, out TornadoSound);
                }
                else
                {
                    if (SoundEngine.AreSoundsPaused && !soundPaused)
                    {
                        TornadoSound.Pause();
                        soundPaused = true;
                    }
                    else if (!SoundEngine.AreSoundsPaused && soundPaused)
                    {
                        TornadoSound.Resume();
                        soundPaused = false;
                    }
                    TornadoSound.Position = Projectile.Center;
                }
            }
            Visuals();
        }
        public override void Kill(int timeLeft)
        {
            if (Projectile.timeLeft < 2)
            {
                TornadoSound.Stop();
            }
        }
        private void Visuals()
        {
            // So it will lean slightly towards the direction it's moving
            Projectile.rotation = Projectile.velocity.X * 0.05f;

            int frameSpeed = 5;

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
            
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        }
    }
}