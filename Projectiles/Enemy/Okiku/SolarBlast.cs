using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class SolarBlast : Projectiles.VFX.DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Solar Blast");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 1200;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;

            trailCollision = true;
            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SolarBlast", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 100);
        }

        bool playedSound = false;
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 2f }, Projectile.Center);
                playedSound = true;
            }
        }


        public override float CollisionWidthFunction(float progress)
        {
            if (progress > 0.9)
            {
                return 0;
            }
            if (progress > 0.8)
            {
                return MathHelper.Lerp(45, 0, (progress - 0.8f) / 0.1f);
            }
            else if (progress > 0.7)
            {
                return MathHelper.Lerp(0, 45, (progress - 0.7f) / 0.1f);
            }

            return 0;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = (int)(trailPositions.Count * 23f / 32f);
            collisionPadding = trailPositions.Count / 8;
            visualizeTrail = false;
            timeFactor++;
            if (CalculateLength() < 500)
            {
                trailWidth = (int)trailCurrentLength / 2;
            }
            else
            {
                trailWidth = 250;
            }
            trailMaxLength = 1500;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(timeFactor);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
