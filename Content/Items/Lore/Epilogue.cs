using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Scrolls;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Lore
{
    class Epilogue : ModItem
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DeathStrikeScroll));
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
        }

        private static bool GwynDefeated => tsorcRevampWorld.NewSlain.ContainsKey(new Terraria.ModLoader.Config.NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()));

        public override bool CanUseItem(Player player)
        {
            return GwynDefeated && !CreditsRoll.IsQueuedOrShowing;
        }

        // Rereading the Epilogue replays the ending for this player alone.
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                CreditsRoll.Queue(0);
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (GwynDefeated)
            {
                tooltips.Add(new TooltipLine(Mod, "ReplayCredits", LangUtils.GetTextValue("Cutscenes.Credits.ReplayHint")));
            }

            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Text.Contains("obscured") || tooltips[i].Text.Contains("模糊不清"))
                {
                    if (GwynDefeated)
                    {
                        tooltips[i].Text = LangUtils.GetTextValue("Items.Epilogue.Obscured");
                    }
                    else
                    {
                        tooltips[i].OverrideColor = Main.DiscoColor;
                    }
                }
            }
        }
    }
}
