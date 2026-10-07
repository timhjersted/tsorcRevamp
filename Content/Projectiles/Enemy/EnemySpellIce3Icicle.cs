using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    class EnemySpellIce3Icicle : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Ice3Icicle));
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 88;
            Projectile.hostile = true;
            Projectile.penetrate = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.coldDamage = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Spawned by Ice 3 balls from Shadow Mage and Marik as well; only shots traced back to The Sorrow build Frost.
            int sourceNPCType = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().SourceNPCType;

            if (sourceNPCType == ModContent.NPCType<NPCs.Bosses.TheSorrow>())
            {
                Buffs.Debuffs.FrostBuildup.Apply(target, NPCs.Bosses.TheSorrow.FrostBuildupPerHit);
            }
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, TorchID.Ice);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
