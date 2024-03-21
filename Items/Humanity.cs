using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items
{
    class Humanity : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 24;
            Item.rare = ItemRarityID.Green;
            Item.value = 75000;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!Main.mouseLeft)
            {
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }
        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
             return ((modPlayer.CurseActive && player.altFunctionUse < 2) || modPlayer.powerfulCurseActive && player.altFunctionUse == 2);
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (Main.myPlayer == player.whoAmI)
            {
                switch (player.altFunctionUse)
                {
                    case 0:
                        {
                            modPlayer.CalculateCurseStats(false);
                            break;
                        }
                    case 1:
                        {
                            modPlayer.CalculateCurseStats(false);
                            break;
                        }
                    case 2:
                        {
                            modPlayer.CalculateCurseStats(true);
                            break;
                        }
                }
            }
            return true;
        }


        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1 && modPlayer.CurseActive)
            {// if we find one
             //insert the extra tooltip line
                
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CurseStats", LangUtils.GetTextValue("Items.Humanity.CurseStats", (int)(modPlayer.CurseMaxLifeMultiplier * ((modPlayer.CurseMaxLifeMultiplier > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.CurseLifeRegenerationBonus * ((modPlayer.CurseLifeRegenerationBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)MathF.Round((modPlayer.CurseDefenseBonus * ((modPlayer.CurseDefenseBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f))), (int)(modPlayer.CurseResistanceBonus * ((modPlayer.CurseResistanceBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.CurseDamageBonus * ((modPlayer.CurseDamageBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.CurseAttackSpeedBonus * ((modPlayer.CurseAttackSpeedBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.CurseMovementSpeedBonus * ((modPlayer.CurseMovementSpeedBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)))));
                ttindex++;
            }
            if (ttindex != -1 && modPlayer.powerfulCurseActive)
            {// if we find one
             //insert the extra tooltip line

                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "powerfulCurseStats", LangUtils.GetTextValue("Items.Humanity.powerfulCurseStats", (int)(modPlayer.powerfulCurseMaxLifeMultiplier * ((modPlayer.powerfulCurseMaxLifeMultiplier > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.powerfulCurseLifeRegenerationBonus * ((modPlayer.powerfulCurseLifeRegenerationBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)MathF.Round((modPlayer.powerfulCurseDefenseBonus * ((modPlayer.powerfulCurseDefenseBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f))), (int)(modPlayer.powerfulCurseResistanceBonus * ((modPlayer.powerfulCurseResistanceBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.powerfulCurseDamageBonus * ((modPlayer.powerfulCurseDamageBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.powerfulCurseAttackSpeedBonus * ((modPlayer.powerfulCurseAttackSpeedBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)), (int)(modPlayer.powerfulCurseMovementSpeedBonus * ((modPlayer.powerfulCurseMovementSpeedBonus > 0) ? modPlayer.CursePositiveStatsMultiplier : 1f)))));
            }
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Color lightColor, Microsoft.Xna.Framework.Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            spriteBatch.Draw(texture, Item.position - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), Color.White, 0f, new Vector2(0, 4), Item.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
