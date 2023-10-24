using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class NuclearMushroom : ModProjectile
    {
        public bool Activated;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
            Main.projFrames[Projectile.type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;

            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100 * 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.knockBack = 0f;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }
        public int BuiltSoundStyle;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.damage = 0;
            Activated = false;
            BuiltSoundStyle = Main.rand.Next(1, 4);
            switch (BuiltSoundStyle)
            {
                case 1:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBuilt1") with { Volume = 1f });
                        break;
                    }
                case 2:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBuilt2") with { Volume = 1f });
                        break;
                    }
                case 3:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBuilt3") with { Volume = 1f });
                        break;
                    }
            }
            player.statMana -= (int)(OmegaSquadRifle.BaseShroomManaCost * player.manaCost);
            player.ManaEffect(-(int)(OmegaSquadRifle.BaseShroomManaCost * player.manaCost));
            player.manaRegenDelay = MeleeEdits.ManaDelay;
        }
        public override void AI()
        {
            int frameSpeed;

            Projectile.frameCounter++;

            if (!Activated)
            {
                frameSpeed = 17;

                if (Projectile.frameCounter >= frameSpeed && Projectile.frame < Main.projFrames[Projectile.type] - 3)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
            }
            else if (Activated)
            {
                frameSpeed = 10;
                if (Projectile.frameCounter >= frameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 9;
                }
            }

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3() * 1f);
        }
        public override bool? CanDamage()
        {
            if (Projectile.timeLeft > 100 * 60 - OmegaSquadRifle.ShroomSetupTime * 60)
            {
                return false;
            }
            Activated = true;
            return null;
        }
        public int BoomSoundStyle;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            BoomSoundStyle = Main.rand.Next(1, 4);
            switch (BoomSoundStyle)
            {
                case 1:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBoom1") with { Volume = 1f });
                        break;
                    }
                case 2:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBoom2") with { Volume = 1f });
                        break;
                    }
                case 3:
                    {
                        SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShroomBoom3") with { Volume = 1f });
                        break;
                    }
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NuclearMushroomExplosion>(), Projectile.damage / 2, Projectile.knockBack * 10, Projectile.owner);
        }
    }
}