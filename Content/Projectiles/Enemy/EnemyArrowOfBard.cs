using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>The Assassin's heavy arrow. Vanilla arrow AI (straight 15t, then +0.1 px/t² gravity); green wake in
    /// flight; dies on the first player it hits instead of piercing on through.</summary>
    public class EnemyArrowOfBard : ModProjectile
    {
        // Use the PROJECTILE sprite (oriented for aiStyle-1 velocity rotation), same as the friendly
        // ArrowOfBardProjectile. The old item sprite (Items/Ammo/ArrowOfBard) is drawn diagonally, so under the
        // arrow's velocity-rotation it rendered pointing backwards.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(ArrowOfBardProjectile));

        // Sparse travel dust: one green mote every TrailDustInterval ticks, matching the shaft and the Assassin's flash.
        private const int TrailDustInterval = 3;
        private const int KillWoodDustCount = 10;
        private const int KillGreenDustCount = 8;

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 5;
            Projectile.height = 10;
            Projectile.scale = 0.8f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
        }

        public override void PostAI()
        {
            if (Main.dedServ || Projectile.timeLeft % TrailDustInterval != 0)
            {
                return;
            }

            Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Projectile.velocity * 0.1f, 100, default, 0.6f);
            trail.noGravity = true;
        }

        // Hostile projectiles never spend penetrate on players, so end it here. This runs on the hit player's machine;
        // tsorcGlobalProjectile relays the kill to the server in multiplayer.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        // Every ending (tile, player, timeout) splinters the shaft and puffs green.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

            for (int i = 0; i < KillWoodDustCount; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.WoodFurniture);
            }

            for (int i = 0; i < KillGreenDustCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, burstVelocity, 100, default, 0.9f);
                mote.noGravity = true;
            }
        }
    }
}
