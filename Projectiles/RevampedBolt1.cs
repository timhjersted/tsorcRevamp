using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
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
            int? closestNPCIndex = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
            if(closestNPCIndex != null)
            {
                NPC newTarget = Main.npc[closestNPCIndex.Value];
                Vector2 direction = UsefulFunctions.GenerateTargetingVector(Projectile.Center, newTarget.Center, 1);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction, ModContent.ProjectileType<RevampedBolt1>(), (int)(Projectile.damage * 1.5f), 0.5f, Projectile.owner);
            }           
            
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 120);
            }
        }
        public override void Kill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<Bolt1Bolt>(), (this.Projectile.damage), 3.5f, Projectile.owner);
            
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(Projectile.type);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Redraw the projectile with the color not influenced by light
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
            

            return true;
        }
    }

}
