using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public abstract class CharmRuneterraOrb : ModProjectile
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract float Scale { get; }
        public abstract int CooldownType { get; }
        public abstract int DebuffType { get; }
        public abstract string SoundPath { get; }
        public abstract Color LightColor { get; }
        public abstract int dustID { get; }
        public int FrameSpeed = 5;
        public override void SetStaticDefaults()
        {
            // These lines facilitate the trail drawing
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
            Projectile.width = Width; // The width of your projectile
            Projectile.height = Height; // The height of your projectile
            Projectile.friendly = true; // Deals damage to enemies
            Projectile.scale = Scale;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true; // Used for hit cooldown changes in the ai hook
            Projectile.localNPCHitCooldown = -1; // This facilitates custom hit cooldown logic
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            player.AddBuff(CooldownType, OrbOfFlame.FireballCD * 60);
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "CharmCast") with { Volume = OrbOfDeception.OrbSoundVolume * 2 });
            Projectile.velocity *= 0.75f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            Rotation();

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= FrameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Lighting.AddLight(Projectile.Center, LightColor.ToVector3() * 1.5f);
            Dust.NewDust(Projectile.TopLeft, Width, Height, dustID, 0, 0, 150, default, 0.5f);
        }
        public virtual void Rotation()
        {
            if (Projectile.velocity.X < 0)
            {
                Projectile.spriteDirection = -1;
                Projectile.rotation -= MathHelper.Pi;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            target.AddBuff(DebuffType, OrbOfFlame.FireballDuration * 60);
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "CharmHit") with { Volume = OrbOfDeception.OrbSoundVolume * 4 });
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage += OrbOfFlame.FireballDmgMod / 100f;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * OrbOfFlame.FireballHPPercentDmg / 100f, OrbOfFlame.FireballHPDmgCap);
        }
    }
}