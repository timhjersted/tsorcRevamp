using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class PowerBoltProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.tileCollide = true;
            Projectile.width = 10;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            if (Main.rand.NextFloat() < 0.33f)
            {
                Explode();
            }
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 1) 
            {
                Projectile.localAI[0] = 0; 
                Explode();
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.rand.NextFloat() < 0.33f)
            {
                Projectile.localAI[0] = 1;
            }
        }

        private void Explode()
        {
            int explosionRadius = 100;

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, Scale: 1.5f);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, Scale: 1.5f);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(Projectile.Center) <= explosionRadius)
                {
        
                    int explosionDamage = (int)(Projectile.damage * 0.33f);

                    NPC.HitInfo hitInfo = new NPC.HitInfo
                    {
                        Damage = explosionDamage,
                        Knockback = 0f,
                        HitDirection = Projectile.Center.X < npc.Center.X ? 1 : -1
                    };

                    npc.StrikeNPC(hitInfo, fromNet: false);
                }
            }
        }
    }
}