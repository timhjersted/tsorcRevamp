using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.PhotonicDownpour
{
    class DefenseDroneFire : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Death Laser");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.timeLeft = 600;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;

            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.CursedInferno, 100);
        }

        bool playedSound = false;
        public override void AI()
        {
            Projectile.damage = 0;
            base.AI();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.5f }, Projectile.Center);
                playedSound = true;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (UsefulFunctions.IsProjectileSafeToFuckWith(i) && Main.projectile[i].Colliding(Main.projectile[i].Hitbox, Projectile.Hitbox))
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.projectile[i].Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Projectile.owner, 300, 30);
                    }

                    Main.projectile[i].Kill();
                    NetMessage.SendData(MessageID.KillProjectile, number: i);
                    NetMessage.SendData(MessageID.SyncProjectile, number: i);

                    Projectile.Kill();
                    NetMessage.SendData(MessageID.KillProjectile, number: Projectile.whoAmI);
                    NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
                }
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 9;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 5;
            visualizeTrail = false;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = new Color(0.5f, 0.2f, 0.1f, 1.0f);
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
