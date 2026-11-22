using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.FriendlyPuppets;

namespace tsorcRevamp.Content.Projectiles
{

    public class MasterBuster : ModProjectile
    {
        private const float MAX_CHARGE = 1f;
        private const float MOVE_DISTANCE = 90f;

        public float Distance
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => Charge == MAX_CHARGE;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.damage = 25;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (IsAtMaxCharge)
            {
                Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MasterBuster];
                Vector2 origin = MirrorPuppetProjectile.GetOwnerVisualCenter(Projectile);
                DrawLaser(Main.spriteBatch, texture, origin,
                    Projectile.velocity, 10, Projectile.damage, -1.57f, 1f, 2000f, Color.White, (int)MOVE_DISTANCE);
            }

            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50)
        {
            float r = unit.ToRotation() + rotation;

            // Draws the laser 'body'
            for (float i = transDist; i <= Distance; i += step)
            {


                Color c = Color.White;
                var origin = start + i * unit;

                Main.EntitySpriteDraw(texture, origin - Main.screenPosition,
                    new Rectangle(0, 26, 26, 30), i < transDist ? Color.Transparent : c, r,
                    new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);

            }

            // Draws the laser 'tail'
            Main.EntitySpriteDraw(texture, start + unit * (transDist - step) - Main.screenPosition,
                new Rectangle(0, 0, 26, 30), Color.White, r, new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);

            // Draws the laser 'head'
            Main.EntitySpriteDraw(texture, start + (Distance + step) * unit - Main.screenPosition,
                new Rectangle(0, 52, 26, 30), Color.White, r, new Vector2(26 * .5f, 30 * .5f), 2.9f, 0, 0);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsAtMaxCharge)
            {
                return false;
            }

            Vector2 origin = MirrorPuppetProjectile.GetOwnerVisualCenter(Projectile);
            Vector2 end = origin + Projectile.velocity * Distance;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, end, 22, ref point);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            MirrorPuppetProjectile tag = Projectile.GetGlobalProjectile<MirrorPuppetProjectile>();
            bool fromPuppet = tag.FromMirrorPuppet;
            bool localOwner = Projectile.owner == Main.myPlayer;

            // Remote puppet beams follow the synced NPC. Only the firing client has the puppet's input and mana.
            if (localOwner || !fromPuppet)
            {
                bool puppetReleased = fromPuppet && (!MirrorPuppetSwap.Active || !player.controlUseItem);

                if (puppetReleased || !player.channel)
                {
                    Projectile.Kill();
                    return;
                }

                Item weapon = player.HeldItem;

                if (Main.GameUpdateCount % 10 == 0 && !player.CheckMana(weapon, weapon.mana, pay: true, blockQuickMana: fromPuppet))
                {
                    Projectile.Kill();
                    return;
                }

                UpdatePlayer(player, fromPuppet);
            }

            if (fromPuppet && tag.GetFiringPuppet(Projectile) == null)
            {
                return;
            }

            // Draw the puppet beam in the projectile pass. The player keeps its held-projectile layer.
            Projectile.hide = !fromPuppet;
            Vector2 origin = MirrorPuppetProjectile.GetOwnerVisualCenter(Projectile);
            Projectile.position = origin + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;
            Charge = MAX_CHARGE;
            Distance = 2000f;

            if (Main.dedServ)
            {
                return;
            }

            SpawnDusts(origin);
            CastLights();

            if (Main.GameUpdateCount % 8 == 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/MasterBuster"), origin);
            }
        }

        private void SpawnDusts(Vector2 origin)
        {
            Vector2 chargePosition = origin + Projectile.velocity * (MOVE_DISTANCE - 20f) - new Vector2(10f, 10f);
            Vector2 chargeVelocity = Projectile.velocity * 0.5f;
            Dust chargeDust = Dust.NewDustDirect(chargePosition, 20, 20, DustID.Electric, chargeVelocity.X, chargeVelocity.Y);
            chargeDust.velocity = Main.rand.NextVector2CircularEdge(2.7f, 2.7f);
            chargeDust.noGravity = true;
            chargeDust.scale = Main.rand.Next(10, 20) * 0.05f;

            Vector2 dustPosition = origin + Projectile.velocity;

            for (int i = 0; i < 2; i++)
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Vector2 dustVelocity = Projectile.velocity.RotatedBy(side * MathHelper.PiOver2);
                dustVelocity *= Main.rand.NextFloat(1f, 1.8f);
                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, DustID.Electric, dustVelocity.X, dustVelocity.Y);
                dust.noGravity = true;
                dust.scale = 1.2f;
                Vector2 beamEndVelocity = Projectile.velocity * Distance;
                Dust.NewDustDirect(origin, 0, 0, DustID.ManaRegeneration, beamEndVelocity.X, beamEndVelocity.Y, 255, Color.White, 10f);
            }

            for (int j = 0; j < 100; j++)
            {
                if (Main.rand.NextBool(5))
                {
                    Vector2 alongBeam = origin + Projectile.velocity * (Distance * j / 100f);
                    Vector2 offset = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(8));
                    Vector2 spawnPosition = alongBeam + offset - Vector2.One * 4f;
                    Dust dust = Dust.NewDustDirect(spawnPosition, 8, 8, DustID.HallowSpray, 0f, 0f, 125, Color.LightBlue, 2f);
                    dust.velocity = Vector2.Zero;
                    dust.noGravity = true;
                    dust.rotation = Projectile.rotation;
                }
            }
        }

        private void UpdatePlayer(Player player, bool fromPuppet)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = Main.MouseWorld - player.Center;
                Projectile.velocity = aim.SafeNormalize(Projectile.velocity);
                Projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
                Projectile.netUpdate = true;
            }

            Vector2 recoilAim = Main.MouseWorld - player.position;
            Vector2 recoilDirection = recoilAim.SafeNormalize(Projectile.velocity);
            player.velocity.X = Math.Clamp(player.velocity.X - recoilDirection.X * 560f / 1250f, -32f, 32f);
            player.velocity.Y = Math.Clamp(player.velocity.Y - recoilDirection.Y * 560f / 1000f, -32f, 32f);
            int facing = Projectile.direction;
            player.ChangeDir(facing);

            if (!fromPuppet)
            {
                player.heldProj = Projectile.whoAmI;
            }

            player.itemTime = 2;
            player.itemAnimation = 2;
            Vector2 itemDirection = Projectile.velocity * facing;
            player.itemRotation = itemDirection.ToRotation();
        }

        private void CastLights()
        {
            // Cast a light along the line of the laser
            DelegateMethods.v3_1 = new Vector3(0.8f, 0.8f, 1f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 26, DelegateMethods.CastLight);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = Terraria.ID.TileID.Sets.TileCutIgnore.None;
            Vector2 unit = Projectile.velocity;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}
