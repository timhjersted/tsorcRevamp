using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Swords
{
    public class DrakarthusDagger : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.height = Projectile.width = 16;
            Projectile.timeLeft = 900; //15 seconds, the cooldown
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 5;
            Projectile.light = 0.6f;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            DrawOriginOffsetX = DrawOriginOffsetY = -10;
        }

        public bool Stuck
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }
        public int TargetWhoAmI
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            Stuck = true;
            return false;
        }


        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Stuck = true;
            TargetWhoAmI = target.whoAmI;
            Projectile.velocity = (target.Center - Projectile.Center) * 0.75f;
            Projectile.netUpdate = true;
            Projectile.damage = 0;
        }


        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45);
            if (!Stuck) return;

            Projectile.tileCollide = false;

            int target = TargetWhoAmI;
            if (target > 0 && target <= Main.maxNPCs)
            {
                if (Main.npc[target].active && Main.npc[target].life > 0)
                {
                    NPC npc = Main.npc[target];
                    Projectile.Center = npc.Center - Projectile.velocity * 2.5f;
                    Projectile.gfxOffY = npc.gfxOffY;

                }
                else Projectile.Kill();
            }
            else
            {
                Projectile.position -= Projectile.velocity; //keep it in place
            }
        }

    }
}
