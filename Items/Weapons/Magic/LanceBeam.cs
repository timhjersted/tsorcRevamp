using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class LanceBeam : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Channel a storm of razor-sharp glowing shards which shred enemy defense");
        }

        public override void SetDefaults()
        {

            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.damage = 45;
            Item.autoReuse = true;
            Item.knockBack = (float)4;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)20;
            Item.crit = 2;
            Item.mana = 100;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;

            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.RadiantGlimmer>();
        }

        //Only one allowed at a time
        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Magic.RadiantGlimmer>()] == 0)
            {                
                return true;
            }
            else
            {
                return false;
            }
        }

        //Dust effects in hand

        int dustIndex = 0;
        int[] dustArray = new int[100];
        float[] dustRotationArray = new float[100];
        bool[] dustActiveArray = new bool[100];
        public override void HoldItem(Player player)
        {           

            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Magic.RadiantGlimmer>()] == 0)
            {
                dustArray[dustIndex] = Dust.NewDustPerfect(UsefulFunctions.GetPlayerHandOffset(player) + Main.rand.NextVector2CircularEdge(6, 6), DustID.PurificationPowder, Vector2.Zero, 0, default, 0.75f).dustIndex;
                dustRotationArray[dustIndex] = UsefulFunctions.Aim(player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, 0), Main.dust[dustArray[dustIndex]].position, 1).ToRotation();
                dustActiveArray[dustIndex] = true;
                dustIndex++;
                if (dustIndex >= 100)
                {
                    dustIndex = 0;
                }

                for (int i = 0; i < 100; i++)
                {
                    if (!Main.dust[dustArray[dustIndex]].active)
                    {
                        dustActiveArray[i] = false;
                    }
                    if (dustArray[i] != 0 && dustActiveArray[i])
                    {
                        Main.dust[dustArray[i]].position = UsefulFunctions.GetPlayerHandOffset(player) + dustRotationArray[i].ToRotationVector2() * 6;
                    }
                }
            }
        }
        List<Tuple<float, float, float>> psuedodust;

        //Psuedodust! Iinstead of doing all the normal dust bs, these are made of 4 floats:
        //0: Its x offset
        //1: Its y offset
        //2: Its rotation, for flavor
        //3: Its size, which is also its lifetime
        //4: Its style, since dusts have 3 variants

        //Using these, we can draw enormous amounts of "dust" for nice shimmering effects like this with a very low performance cost
        //But it could be even lower if I moved these calculations to the GPU instead by making a shader. Hmm....

        public PsuedoDust[] psuedoDusts;
        int dustCount = 50;
        float dustRadius = 16;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            dustCount = 100;
            float dustRadius = frame.Width / 2.5f;
            if (psuedoDusts == null || psuedoDusts.Length != dustCount)
            {
                psuedoDusts = new PsuedoDust[dustCount];
                for(int i = 0; i < dustCount - 1; i++)
                {
                    Vector2 drawPosition = position;
                    if (Main.rand.NextBool(5))
                    {
                        drawPosition += Main.rand.NextVector2Circular(dustRadius, dustRadius);
                    }
                    else
                    {
                        drawPosition += Main.rand.NextVector2CircularEdge(dustRadius, dustRadius);
                    }

                    drawPosition.X += frame.Width / 2;
                    drawPosition.Y += frame.Height / 2;
                    psuedoDusts[i] = new PsuedoDust(drawPosition, Main.rand.NextFloatDirection(), Main.rand.NextFloat() * 0.05f, DustID.PurificationPowder, Main.rand.Next(0, 3));
                }
            }

            RasterizerState OverflowHiddenRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            {
                int i = 0;
                do//for (int i = 0; i < dustCount - 1; i++)
                {
                    spriteBatch.Draw((Texture2D)Terraria.GameContent.TextureAssets.Dust, psuedoDusts[i].center, psuedoDusts[i].Frame(), psuedoDusts[i].color, psuedoDusts[i].rotation, new Vector2(5, 5), psuedoDusts[i].size * 0.5f, SpriteEffects.None, 1);
                    psuedoDusts[i].size -= 0.02f;
                    if (psuedoDusts[i].size <= 0)
                    {
                        Vector2 drawPosition = position;
                        drawPosition.X += frame.Width / 2;
                        drawPosition.Y += frame.Height / 2;
                        if (Main.rand.NextBool(5))
                        {
                            psuedoDusts[i] = new PsuedoDust(drawPosition + Main.rand.NextVector2Circular(dustRadius, dustRadius), Main.rand.NextFloatDirection(), 3 * Main.rand.NextFloat() / 1.4f, DustID.PurificationPowder, Main.rand.Next(0, 3));
                        }
                        else
                        {
                            psuedoDusts[i] = new PsuedoDust(drawPosition + Main.rand.NextVector2CircularEdge(dustRadius, dustRadius), Main.rand.NextFloatDirection(), 3 * Main.rand.NextFloat() / 2f, DustID.PurificationPowder, Main.rand.Next(0, 3));
                        }
                    }
                    i++;
                } while (i < dustCount - 1);

            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            

            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, new Vector3(0, 0.4f, 1f));

            float lightingQuality = 50;
            for(float i = 0; i < lightingQuality; i++)
            {
                Lighting.AddLight(Item.Center + new Vector2(0, 250).RotatedBy(MathHelper.TwoPi * i / lightingQuality), new Vector3(0, 0.4f, 1f));
            }

            //dustCount = 5000;
            //float dustRadius = 250;
            dustCount = 100;
            dustRadius = 20;
            if (psuedoDusts == null || psuedoDusts.Length != dustCount)
            {
                psuedoDusts = new PsuedoDust[dustCount];
                for (int i = 0; i < dustCount - 1; i++)
                {
                    Vector2 drawPosition = Item.position;
                    if (Main.rand.NextBool(5))
                    {
                        drawPosition += Main.rand.NextVector2Circular(dustRadius, dustRadius);
                    }
                    else
                    {
                        drawPosition += Main.rand.NextVector2CircularEdge(dustRadius, dustRadius);
                    }

                    psuedoDusts[i] = new PsuedoDust(drawPosition, Main.rand.NextFloatDirection(), Main.rand.NextFloat(), DustID.PurificationPowder, Main.rand.Next(0, 3));
                }
            }

            RasterizerState OverflowHiddenRasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                ScissorTestEnable = true
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            {
                int i = 0;
                do//for (int i = 0; i < dustCount - 1; i++)
                {
                    psuedoDusts[i].Draw(spriteBatch);
                    psuedoDusts[i].size -= 0.01f;
                    if (psuedoDusts[i].size <= 0)
                    {
                        Vector2 drawPosition = Item.position;
                        if (Main.rand.NextBool(5))
                        {
                            psuedoDusts[i] = new PsuedoDust(drawPosition + Main.rand.NextVector2Circular(dustRadius, dustRadius), Main.rand.NextFloatDirection(), 2 * Main.rand.NextFloat() / 1.4f, DustID.PurificationPowder, Main.rand.Next(0, 3));
                        }
                        else
                        {
                            psuedoDusts[i] = new PsuedoDust(drawPosition + Main.rand.NextVector2CircularEdge(dustRadius, dustRadius), Main.rand.NextFloatDirection(), 3 * Main.rand.NextFloat() / 2f, DustID.PurificationPowder, Main.rand.Next(0, 3));
                        }
                    }
                    i++;
                } while (i < dustCount - 1);

            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CrystalShard, 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 20);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(),5);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
