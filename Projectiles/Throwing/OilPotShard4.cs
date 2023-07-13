using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.Projectiles.Throwing
{
    class OilPotShard4 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.timeLeft = 240;
            Projectile.aiStyle = ProjAIStyleID.ThrownProjectile;
            Projectile.knockBack = 9;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.ContinuouslyUpdateDamageStats = true;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Oiled, OilPot.OiledDurationInSeconds * 60);
        }
    }
}
