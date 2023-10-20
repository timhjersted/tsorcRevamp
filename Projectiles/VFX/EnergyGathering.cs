using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    public class EnergyGathering : DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
            Projectile.width = 10;
            Projectile.height = 250;

            trailCollision = false;
            trailWidth = 55;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 850;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        Color EffectColor
        {
            get
            {
                return UsefulFunctions.ColorFromFloat(Projectile.ai[1]);
            }
        }
        float Radius
        {
            get
            {
                return Projectile.ai[0];
            }
        }

        bool fading = false;
        bool initialized = false;
        float widthPercent = 0.01f;
        int realWidth;
        Vector2 targetPoint;
        float speed = 0.5f;
        public override void AI()
        {
            targetPoint = Main.npc[(int)Projectile.ai[0]].Center;

            if (!initialized)
            {
                float radius = 400;
                if (Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.TheRage>())
                {
                    //The rage fight is already chaotic as fuck, these lessen that very slightly
                    radius = 200;
                    trailWidth = 35;
                    Projectile.timeLeft = 120;
                    speed = 1;
                }
                if (Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.TheSorrow>())
                {
                    radius = 200;
                    trailWidth = 35;
                    Projectile.timeLeft = 120;
                    speed = 0.75f;
                }
                realWidth = trailWidth;
                trailWidth = 0;
                Projectile.Center += Main.rand.NextVector2CircularEdge(radius, radius);
                Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, targetPoint, Main.rand.NextFloat(20, 40)).RotatedBy(MathHelper.Pi / 3f);
                initialized = true;
            }


            if (widthPercent < 1)
            {
                widthPercent *= 1.06f;
                if (widthPercent > 1)
                {
                    widthPercent = 1;
                }
            }
            if (Projectile.timeLeft < 30)
            {
                widthPercent = Projectile.timeLeft / 30f;
            }

            trailWidth = (int)(realWidth * widthPercent);

            if (!fading)
            {
                UsefulFunctions.SmoothHoming(Projectile, targetPoint, speed, 27, bufferZone: false);
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (Vector2.Distance(Projectile.Center, targetPoint) < 100)
                {
                    Projectile.timeLeft = 60;
                    fading = true;
                }
            }
            else
            {
                fadeOut = 1f - Projectile.timeLeft / 60f;
            }



            base.AI();

            if (dying && trailPositions.Count > 0)
            {
                trailPositions.RemoveAt(0);
                trailRotations.RemoveAt(0);
            }
        }

        float baseNoiseUOffset;
        public override void SetEffectParameters(Effect effect)
        {
            if (baseNoiseUOffset == 0)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);
            //effect.Parameters["secondaryNoise"].SetValue(noiseTexture);

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly);
            Color shaderColor = EffectColor;
            effect.Parameters["slashCenter"].SetValue(Color.White.ToVector4());
            effect.Parameters["slashEdge"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return base.PreDraw(ref lightColor);
        }
    }
}