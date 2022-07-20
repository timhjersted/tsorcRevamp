using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class RevampedBolt1 : Enemy.EnemyGenericLaser
    {
        //WIP for the new bolt tome attack that Neph made

        public override void SetDefaults()
        {

        }
        public override void AI()
        {

            
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            
            /*for()
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<Bolt1Bolt>(), (this.Projectile.damage), 3.5f, Projectile.owner);
            */
            if (Main.rand.NextBool(4))
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 120);
            }
        }
        public override void Kill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<Bolt1Bolt>(), (this.Projectile.damage), 3.5f, Projectile.owner);
            
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
        }
    }

}
