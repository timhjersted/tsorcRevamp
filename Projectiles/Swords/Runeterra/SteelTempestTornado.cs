using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
    public class SteelTempestTornado : ModProjectile
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
            Projectile.timeLeft = 800;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.DD2_BookStaffTwisterLoop, Projectile.Center);
            Player player = Main.player[Projectile.owner];
            Projectile.damage = (int)(player.GetWeaponDamage(player.HeldItem) * (1.5f + player.GetTotalAttackSpeed(DamageClass.Melee))); //scales with attack speed since higher melee speed increases the travelling speed, therefore giving it less time to get hits off
        }
        public override void Kill(int timeLeft)
        {
            SoundEngine.FindActiveSound(SoundID.DD2_BookStaffTwisterLoop);
            SoundEngine.StopTrackedSounds();
        }
        public override void AI()
        {
            Visuals();
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
        }
    }
}