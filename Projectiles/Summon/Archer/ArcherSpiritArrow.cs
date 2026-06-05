using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    public class ArcherSpiritArrow : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FrostburnArrow;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Archer Spirit Arrow");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FrostburnArrow);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            AIType = ProjectileID.FrostburnArrow;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180); // Inflicts Frostburn for 3 seconds
        }
    }
}
