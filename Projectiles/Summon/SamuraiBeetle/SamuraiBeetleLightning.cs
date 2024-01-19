using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    public class SamuraiBeetleLightning : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        SoundStyle ThunderLoopStyle;
        SlotId ThunderLoopID;
        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 2000;
            Projectile.friendly = true;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Summon;
            ThunderLoopStyle = new SoundStyle(SamuraiBeetleProjectile.SoundPath + "heavy-thunder-loop") with { Volume = 1f, MaxInstances = 1 };
        }
        public override void OnSpawn(IEntitySource source)
        {
            if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound))
            {
                ThunderLoopID = SoundEngine.PlaySound(ThunderLoopStyle);
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound))
            {
                ActiveSound.Stop();
            }
        }
        public bool SoundPlayed = false;
        public override void AI()
        {
            if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var ActiveSound) && !SoundPlayed)
            {
                ThunderLoopStyle = new SoundStyle(SamuraiBeetleProjectile.SoundPath + "heavy-thunder-out") with { Volume = 1f, MaxInstances = 1 };
                ThunderLoopID = SoundEngine.PlaySound(ThunderLoopStyle);
                SoundPlayed = true;
            } 
            else if (!SoundEngine.TryGetActiveSound(ThunderLoopID, out var activeSound) && SoundPlayed)
            {
                Projectile.Kill();
            }


            int frameSpeed = 3;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = 0;
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CCShock>(), 600);
        }
    }
}
