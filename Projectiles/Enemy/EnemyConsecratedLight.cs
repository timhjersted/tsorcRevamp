using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // A ground-sweeping beam of holy light: starts aimed from the mouth down to the ground just
    // ahead of the caster, then sweeps 160 degrees - slowly at first, accelerating - in whichever
    // direction the caster is currently facing, so it reads the same walking forward or backward.
    public class EnemyConsecratedLight : GenericLaser
    {
        public override string Texture => base.Texture;

        const float StartLeanDegrees = 30f;
        const float TotalSweepDegrees = 160f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 40;
            FiringDuration = 90;
            MaxCharge = 40;
            LaserLength = 3000;
            LaserColor = new Color(255, 230, 90);
            LightColor = new Color(255, 240, 160);
            TileCollide = true;
            LaserDust = DustID.GoldFlame;
            LineDust = true;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.ConsecratedLightTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSize = 1f;
            LaserSound = SoundID.Item122 with { Volume = 0.6f, Pitch = 0.3f };
        }

        bool initializedDirection = false;
        Vector2 baseDirection;
        int sweepDirection = 1;

        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[1]];
            if (owner == null || !owner.active)
            {
                Projectile.active = false;
                return;
            }

            // Origin tracks near the head - offset forward/up from center, roughly where the
            // mouth is regardless of exactly how the long neck happens to be swaying that frame.
            LaserOffset = new Vector2(owner.direction * owner.width * 0.35f, -owner.height * 0.3f);

            if (!initializedDirection)
            {
                sweepDirection = owner.direction;
                baseDirection = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(-StartLeanDegrees * sweepDirection));
                initializedDirection = true;
            }

            if (Charge < MaxCharge)
            {
                // Telegraph: hold the starting mouth-to-ground aim steady so the landing spot is readable.
                Projectile.velocity = baseDirection;

                if (Main.rand.NextBool(3))
                {
                    Vector2 mouthPos = GetOrigin();
                    int dust = Dust.NewDust(mouthPos, 4, 4, DustID.GoldFlame, 0f, 0f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(mouthPos, 4, 4, DustID.WhiteTorch, 0f, 0f, 100, default, 0.9f);
                    Main.dust[dust].noGravity = true;
                }
            }
            else
            {
                // Firing: sweep the full 160 degrees with an ease-in curve, so the rotation
                // starts slow and visibly accelerates as it goes.
                float t = FiringDuration > 0 ? 1f - (float)FiringTimeLeft / FiringDuration : 1f;
                t = MathHelper.Clamp(t, 0f, 1f);
                float eased = t * t;
                float angle = MathHelper.ToRadians(TotalSweepDegrees) * eased * sweepDirection;
                Projectile.velocity = baseDirection.RotatedBy(angle);

                Vector2 mouthPos = GetOrigin();
                int dust = Dust.NewDust(mouthPos, 4, 4, DustID.GoldFlame, 0f, 0f, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    dust = Dust.NewDust(mouthPos, 4, 4, DustID.WhiteTorch, 0f, 0f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
            }

            base.AI();
        }
    }
}
