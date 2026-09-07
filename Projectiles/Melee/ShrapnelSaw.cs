using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Systems.LethalTempo;

namespace tsorcRevamp.Projectiles.Melee
{
    class ShrapnelSaw : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        Player player
        {
            get
            {
                return Main.player[Projectile.owner];
            }
        }
        private float LtTimer;

        public override void AI()
        {
            Projectile.idStaticNPCHitCooldown = (int)(Shrapnel.BaseHitCooldown / Main.player[Projectile.owner].GetTotalAttackSpeed(DamageClass.Melee));
            
            if (player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                var staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                float staminaCost = (0.1f * modPlayer.WeaponStaminaMult(player.HeldItem)) / player.GetWeaponAttackSpeed(player.HeldItem);

                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= staminaCost;

                staminaPlayer.staminaRegenDelayTimer = 2;
            }
            if (player.HasBuff(ModContent.BuffType<LethalTempo>()))
            {
                LtTimer--;
                if (LtTimer <= 0)
                {
                    Projectile.GetGlobalProjectile<LethalTempoGlobalProjectile>().AppliedLethalTempo = false;
                    LtTimer = Projectile.idStaticNPCHitCooldown;
                }
            }
            
            Projectile.timeLeft = 2;
            Projectile.width = 60;
            Projectile.height = 60;

            if (player.whoAmI == Main.myPlayer)
            {
                Projectile.rotation = UsefulFunctions.Aim(player.Center, Main.MouseWorld, 1).ToRotation();
                Projectile.direction = Main.MouseWorld.X > player.Center.X ? 1 : -1;
                Vector2 rotDir = Projectile.rotation.ToRotationVector2();
                Projectile.Center = player.Center + rotDir * 64;
                if (Projectile.direction == -1)
                {
                    rotDir *= -1;
                }
                player.itemRotation = rotDir.ToRotation();
                player.ChangeDir(Projectile.direction);
                player.itemTime = 2; // Set item time to 2 frames while we are used
                player.itemAnimation = 2; // Set item animation time to 2 frames while we are used
            }


            if (!player.channel || player.noItems || player.CCed)
            {
                Projectile.damage = 0;
                Projectile.Kill();
            }
        }

        public static Texture2D holderTexture;
        public static Texture2D sawTexture;
        public static ArmorShaderData data;
        public float heatingUp;
        public override bool PreDraw(ref Color lightColor)
        {
            heatingUp++;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            UsefulFunctions.EnsureLoaded(ref sawTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeSawBlade");
            UsefulFunctions.EnsureLoaded(ref holderTexture, "tsorcRevamp/Projectiles/Melee/ShrapnelSaw");

            Projectile.frameCounter++;
            if (Projectile.frameCounter == 2)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }

            if (data == null)
            {
                data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            }

            Color shaderColor = Color.Lerp(Color.Black, Color.OrangeRed, heatingUp / 120f);
            data.UseColor(shaderColor);
            data.Apply(null);



            int frameHeight = sawTexture.Height / 4;
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, sawTexture.Width, frameHeight);
            Rectangle holderRectangle = new Rectangle(0, 0, holderTexture.Width, holderTexture.Height);
            Vector2 holderOrigin = holderRectangle.Size() / 2;

            // Redraw the projectile with the color not influenced by light
            Vector2 drawOrigin = new Vector2(sawTexture.Width * 0.5f, sawTexture.Height * 0.25f);
            int shadowFrame = Projectile.frame;
            for (int k = Projectile.oldPos.Length - 1; k >= 0; k--)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition + Projectile.Size / 2f);
                Rectangle shadowRectangle = new Rectangle(0, frameHeight * shadowFrame, sawTexture.Width, frameHeight);
                Color color = Projectile.GetAlpha(Color.White) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(sawTexture, drawPos + new Vector2(0, 30), shadowRectangle, color, 0, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

                shadowFrame--;
                if (shadowFrame < 0)
                {
                    shadowFrame = 3;
                }
            }

            Main.EntitySpriteDraw(sawTexture, Projectile.Center - Main.screenPosition + new Vector2(0, 30), sourceRectangle, Color.White, 0, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            Main.EntitySpriteDraw(holderTexture, Projectile.Center - Main.screenPosition, holderRectangle, Color.White, Projectile.rotation, holderOrigin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300, false);
            target.AddBuff(BuffID.BrokenArmor, 300, false);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 300, false);
            target.AddBuff(BuffID.BrokenArmor, 300, false);
        }
    }
}