using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class BurningStone : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A chunk of meteorite, glowing with heat" +
                                "\nDashing or rolling summons a barrage of homing fireballs" +
                                "\nFireballs scale in power with each boss you kill" +
                                "\nAlso increases damage dealt to burning foes by 5%");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.Expert;
        }


        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "killcount", $"Current count: {((tsorcRevampWorld.Slain == null) ? 0 : tsorcRevampWorld.Slain.Count)}"));
            base.ModifyTooltips(tooltips);
        }
        public override void UpdateEquip(Player player)
        {
            tsorcRevampPlayer ModPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            ModPlayer.BurningStone = true;
            Lighting.AddLight(player.Center, Color.Orange.ToVector3());
            if (ModPlayer.isDodging || player.timeSinceLastDashStarted < 20)
            {
                if (Main.GameUpdateCount % 5 == 0)
                {
                    int? target = UsefulFunctions.GetClosestEnemyNPC(player.Center);

                    if (target != null && Main.npc[target.Value].Distance(player.Center) < 1000)
                    {
                        Vector2 velocity = UsefulFunctions.GenerateTargetingVector(player.Center, Main.npc[target.Value].Center, 10);
                        int damage = tsorcRevampWorld.Slain.Count * 3;
                        if (tsorcRevampWorld.SuperHardMode)
                        {
                            damage *= 2;
                        }

                        for (int i = 0; i < 5; i++)
                        {
                            Dust.NewDustPerfect(player.Center, DustID.InfernoFork, player.velocity, 200, Scale: 0.65f).noGravity = true;
                        }
                        Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, velocity, ModContent.ProjectileType<Projectiles.HomingFireball>(), damage, 0.5f, player.whoAmI);
                    }
                }
            }
        }

        public static Texture2D texture;
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            //Ending then restarting the batch to apply shaders fucks up the inventory drawing. I'm guessing it's because it uses a different view matrix than normal sprite drawing?
            //Todo: Look at vanilla and see what it does when it starts the inventory spritebatch
            /*
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
            data.Apply(null);

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Item.ModItem.Texture);
            }

            spriteBatch.Draw(texture, position, frame, drawColor, 0, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            */
            return true;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
        }
    }
}
