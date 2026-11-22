using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items
{
    class StaminaVessel : ModItem
    {
        public const float PermanentStaminaCap = 250f;
        public const float StaminaPerVessel = 5f;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaPerVessel);

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
            float baseStamina = player.GetModPlayer<tsorcRevampPlayer>().GetStartingClassBaseStamina();
            float permanentStamina = player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax;
            int vesselsToMax = (int)Math.Ceiling((PermanentStaminaCap - baseStamina) / StaminaPerVessel);
            int used = (int)((permanentStamina - baseStamina) / StaminaPerVessel);

            // Keep the player-specific counts on mod-owned lines. The shared tooltip hook refreshes
            // Terraria's TooltipN lines from the static item description after this hook.
            int insertIndex = tooltips.FindIndex(line => line.Mod == "Terraria" && line.Name == "Tooltip0") + 1;
            if (insertIndex == 0)
            {
                insertIndex = tooltips.Count;
            }

            string maxVesselsText = this.GetLocalization("MaxVessels").Format(vesselsToMax);
            string usedCounterText = this.GetLocalization("UsedCounter").Value + used;
            tooltips.Insert(insertIndex, new TooltipLine(Mod, "MaxVessels", maxVesselsText));
            tooltips.Insert(insertIndex + 1, new TooltipLine(Mod, "UsedCounter", usedCounterText));
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
            Texture2D texture = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(StaminaVessel)) + "_Glow");
            spriteBatch.Draw(texture, new Vector2(Item.position.X - Main.screenPosition.X + Item.width * 0.5f, Item.position.Y - Main.screenPosition.Y + Item.height - texture.Height * 0.5f + 2f),
                new Rectangle(0, 0, texture.Width, texture.Height), color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }
}
