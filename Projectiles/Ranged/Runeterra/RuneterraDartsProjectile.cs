using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public abstract class RuneterraDartsProjectile : ModProjectile
    {
        public abstract int ExtraUpdates { get; }
        public abstract int DebuffType { get; }
        public abstract string SoundPath { get; }
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = ProjAIStyleID.SmallFlying;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = ExtraUpdates;

            AIType = ProjectileID.Bat;
            CustomSetDefaults();
        }
        public virtual void CustomSetDefaults()
        {
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hit.Crit)
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "ShotCrit") with { Volume = 0.5f });
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "ShotHit") with { Volume = 0.5f });
            }
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[Projectile.owner];
            target.AddBuff(DebuffType, ToxicShot.DebuffDuration * 60);
        }
    }
}