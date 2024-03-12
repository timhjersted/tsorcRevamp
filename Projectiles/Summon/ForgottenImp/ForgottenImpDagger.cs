using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.ForgottenImp
{
    class ForgottenImpDagger : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 16;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 2;
            Projectile.penetrate = 3;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            Projectile.spriteDirection = Projectile.direction;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Hemorrhage>(), ForgottenImpHalberd.BleedDuration * 60);
        }
    }
}