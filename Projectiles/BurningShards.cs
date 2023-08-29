
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

class BurningShards : ModProjectile
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Barrier");
    }
    public override void SetDefaults()
    {
        DrawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
        Projectile.friendly = true;
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.penetrate = -1;
        Projectile.scale = 1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.alpha = 160;
    }

    List<float> foundIndicies = new List<float>();
    bool initialized = false;
    float shotCooldown = 0;
    public override void AI()
    {
        Projectile.timeLeft = 2;
        if (!initialized)
        {
            for(int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == Projectile.type && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].whoAmI != Projectile.whoAmI)
                {
                    foundIndicies.Add(Main.projectile[i].ai[0]);
                }
            }

            for(int i = 0; i < 5; i++)
            {
                if (foundIndicies.Contains(i))
                {
                    continue;
                }
                else
                {
                    Projectile.ai[0] = i;
                    break;
                }
            }
            initialized = true;
        }

        if(shotCooldown > 0)
        {
            shotCooldown--;
        }

        var player = Main.player[Projectile.owner];

        Vector2 offset = new Vector2(60, 0).RotatedBy((Projectile.ai[0] + 1f) * (Main.GameUpdateCount / 300f) * MathHelper.TwoPi / 5f);
        Projectile.Center = player.Center + offset;
        offset.Normalize();
        Projectile.velocity = 1.5f * offset.RotatedBy(MathHelper.PiOver2);
        Projectile.rotation = 2 * (Main.GameUpdateCount * (Projectile.ai[0] + 1f) / 300f);


        tsorcRevampPlayer ModPlayer = player.GetModPlayer<tsorcRevampPlayer>();

        if (player.dead || !ModPlayer.BurningAura)
        {
            Projectile.Kill();
            return;
        }


        if (Main.GameUpdateCount % 225 == 45 * Projectile.ai[0])
        {
            int? target = UsefulFunctions.GetClosestEnemyNPC(player.Center);
            if (target != null && Main.npc[target.Value].Distance(player.Center) < 1000)
            {
                if (Main.netMode != NetmodeID.Server && Main.player[Projectile.owner] == Main.LocalPlayer)
                {
                    Vector2 velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, Main.npc[target.Value].Center, 10);
                    int damage = 1 + (int)(tsorcRevampWorld.NewSlain.Count * 3f);
                    if (tsorcRevampWorld.SuperHardMode)
                    {
                        damage *= 2;
                    }
                    Projectile.NewProjectile(player.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<Projectiles.HomingFireball>(), damage, 0.5f, player.whoAmI);
                    shotCooldown = 45;
                }
            }
        }

        if (Main.rand.NextBool(10))
        {
            Dust thisDust = Dust.NewDustDirect(Projectile.position, 16, 16, DustID.InfernoFork, 0, 0, 0, default, 0.5f);
            thisDust.noGravity = true;
            thisDust.velocity = Vector2.Zero;
        }
    }

    public static Texture2D texture;
    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
        data.UseColor(Color.Lerp(Color.Black, Color.Orange, shotCooldown / 60f));
        //data.Apply(null);

        SpriteEffects spriteEffects = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }

        if (texture == null || texture.IsDisposed)
        {
            texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
        }

        int frameHeight = texture.Height / 5;
        int startY = frameHeight * (int)Projectile.ai[0];
        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
        Vector2 origin = sourceRectangle.Size() / 2f;
        Main.EntitySpriteDraw(texture,
            Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
        
        return false;
    }

    public override bool? CanDamage()
    {
        return false;
    }
}