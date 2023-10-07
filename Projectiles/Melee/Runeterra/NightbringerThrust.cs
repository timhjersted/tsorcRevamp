using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
	public class NightbringerThrust : ModProjectile
	{
		public bool Hit = false;
		public const int FadeInDuration = 7;
		public const int FadeOutDuration = 4;

		public const int TotalDuration = 32;

		public float CollisionWidth => 10f * Projectile.scale;

		public int Timer
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 12;
        }
        public override void SetDefaults()
		{
			Projectile.Size = new Vector2(18);
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.scale = 1f;
            Projectile.usesOwnerMeleeHitCD = true;
            Projectile.DamageType = DamageClass.Melee;
			Projectile.ownerHitCheck = true; 
			Projectile.extraUpdates = 1; 
			Projectile.timeLeft = 360;
			Projectile.hide = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 2;
        }
        public override void AI()
		{
			Player player = Main.player[Projectile.owner];

			Timer += 1;
			if (Timer >= TotalDuration)
			{
				Projectile.Kill();
				return;
			}
			else
			{
				player.heldProj = Projectile.whoAmI;
			}

			Projectile.Opacity = Utils.GetLerpValue(0f, FadeInDuration, Timer, clamped: true) * Utils.GetLerpValue(TotalDuration, TotalDuration - FadeOutDuration, Timer, clamped: true);

			Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
			Projectile.Center = playerCenter + Projectile.velocity * (Timer - 1f);

            //Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f).ToDirectionInt();

            Projectile.rotation = Projectile.velocity.ToRotation();

            const int HalfSpriteWidth = 244 / 2;//needs adjustments for sprite
            const int HalfSpriteHeight = 50 / 2;

            int HalfProjWidth = Projectile.width / 2;
            int HalfProjHeight = Projectile.height / 2;

            DrawOriginOffsetX = 0;
            DrawOffsetX = -(HalfSpriteWidth - HalfProjWidth);
            DrawOriginOffsetY = -(HalfSpriteHeight - HalfProjHeight);

            float frameSpeed = 3f;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
        }
		public override bool ShouldUpdatePosition()
		{
			return false;
		}

		public override void CutTiles()
		{
			DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = Terraria.ID.TileID.Sets.TileCutIgnore.None;
            Vector2 start = Projectile.Center;
			Vector2 end = start + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
			Utils.PlotTileLine(start, end, CollisionWidth, DelegateMethods.CutTiles);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 start = Projectile.Center;
			Vector2 end = start + Projectile.velocity * 17f;
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref collisionPoint);
		}
        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;

            if (Main.player[Projectile.owner].direction == 1)
            {
            } else if (Main.player[Projectile.owner].direction != 1)
            {
                spriteEffects = SpriteEffects.FlipVertically;
            }
            texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            glowTexture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture + "Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/ThrustHit") with { Volume = 0.75f });
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.itemAnimation = 0;
            player.itemTime = 0;
            if (Hit)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks += 1;
                if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SteelTempestStacks >= 2)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/Nightbringer/TornadoReady") with { Volume = 0.75f });
                }
            }
        }
    }
}
