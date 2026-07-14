using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class StaminaVessel : ModItem
    {
        public const float PermanentStaminaCap = 200f;
        public const float StaminaPerVessel = 5f;

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Permanently increases max stamina by 5%" +
                               "\nStamina maxes out after consuming 10"); */
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 24;
            Item.rare = ItemRarityID.Expert;
            Item.value = 0;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            // Everything here counts from the player's own class starting stamina rather than the generic
            // default. Every class converges on the same PermanentStaminaCap, so the number of vessels it takes
            // to get there differs: Melee starts at 130 and needs 14, Magic starts at 115 and needs 17. Measuring
            // against DefaultStaminaResourceMax reported vessels the player hadn't eaten (and, for Magic, a
            // negative count), and the "maxes out after" figure was a flat 15 baked into the localization.
            float baseStamina = player.GetModPlayer<tsorcRevampPlayer>().GetStartingClassBaseStamina();
            int vesselsToMax = (int)Math.Ceiling((PermanentStaminaCap - baseStamina) / StaminaPerVessel);
            int used = (int)((player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax - baseStamina) / StaminaPerVessel);

            // The localized line carries a {0} for the per-class total. A translation that hasn't been updated
            // yet has no placeholder, and string.Format leaves it alone rather than throwing.
            TooltipLine maxLine = tooltips.Find(line => line.Name == "Tooltip1");
            if (maxLine != null)
            {
                maxLine.Text = string.Format(maxLine.Text, vesselsToMax);
            }

            tooltips.Insert(4, new TooltipLine(Mod, "UsedCounter", Language.GetTextValue("Mods.tsorcRevamp.Items.StaminaVessel.UsedCounter") + used));

        }

        public override bool CanUseItem(Player player)
        {
            return (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax < PermanentStaminaCap);
        }

        public override bool? UseItem(Player player)
        {
            tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            staminaPlayer.staminaResourceMax = Math.Min(PermanentStaminaCap, staminaPlayer.staminaResourceMax + StaminaPerVessel);
            return true;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Lighting.AddLight(Item.Center, 0.15f, 0.25f, 0.15f);

            if (Main.rand.NextBool(20))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(Item.position.X, Item.position.Y), 20, 26, 43, Item.velocity.X, Item.velocity.Y, 100, Color.Yellow, Main.rand.NextFloat(.2f, .4f))];
                dust.velocity *= 0f;
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            Color color = Color.White * 0.5f;
            Texture2D texture = (Texture2D)Mod.Assets.Request<Texture2D>("Items/StaminaVessel_Glow");
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }
}
