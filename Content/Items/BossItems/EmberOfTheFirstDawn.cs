using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.BossItems
{
    /// <summary>
    /// Dropped by Gwyn. A confirmed first use permanently transforms the world into the Primordial world and plays its
    /// cutscene (Systems/PrimordialWorldSystem.cs). Afterwards it returns its user to Prime Gwyn's landing point.
    /// Multiplayer: the using client asks the server, which validates the use and makes every change. The return
    /// teleport is the user's own movement, so the owning client performs it.
    /// </summary>
    class EmberOfTheFirstDawn : ModItem
    {
        const int UseDustCount = 36;

        // A second use within this window confirms the transformation. Local to the using client; nothing to sync.
        const int ConfirmWindowTicks = 300;
        static uint confirmExpiresAt;

        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.consumable = false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string key = "Items.EmberOfTheFirstDawn.Warning";
            if (tsorcRevampWorld.PrimordialAge)
            {
                key = "Items.EmberOfTheFirstDawn.AlreadyReturned";
            }

            TooltipLine stateLine = new TooltipLine(Mod, "PrimordialAgeState", LangUtils.GetTextValue(key));
            stateLine.OverrideColor = new Color(255, 144, 64);
            tooltips.Add(stateLine);
        }

        public override bool CanUseItem(Player player)
        {
            bool gwynSlain = tsorcRevampWorld.BossDefeated(ModContent.NPCType<Gwyn>());
            return gwynSlain && !tsorcRevampWorld.BossAlive && !PrimordialWorldSystem.SceneRunning && !CutsceneSystem.IsActive;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.4f }, player.Center);
            for (int i = 0; i < UseDustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust ember = Dust.NewDustPerfect(player.Center, DustID.GoldFlame, velocity, 40, default, 1.4f);
                ember.noGravity = true;
            }

            Color textColor = new Color(255, 144, 64);

            if (PrimordialWorldSystem.Conversion != PrimordialWorldSystem.ConversionState.None)
            {
                ReturnToLanding(player, textColor);
                return true;
            }

            // First use only warns. The warning must not say what changes, so the transformation stays a surprise.
            bool confirmed = Main.GameUpdateCount < confirmExpiresAt;
            if (!confirmed)
            {
                confirmExpiresAt = Main.GameUpdateCount + ConfirmWindowTicks;
                Main.NewText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.Confirm"), textColor);
                return true;
            }

            confirmExpiresAt = 0;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.UseEmberOfTheFirstDawn);
                packet.Send();
            }
            else
            {
                ResolveUse(player);
            }
            return true;
        }

        /// <summary>Server or single player: a confirmed use from this player.</summary>
        internal static void ResolveUse(Player player)
        {
            PrimordialWorldSystem.RequestTransformation(player);
        }

        // After the transformation the Ember carries its user back to the landing point east of Prime's ring.
        static void ReturnToLanding(Player player, Color textColor)
        {
            bool hasLanding = PrimordialWorldSystem.Site == PrimordialWorldSystem.SiteMode.Adventure && PrimordialWorldSystem.LandingWorld != Vector2.Zero;
            if (!hasLanding)
            {
                Main.NewText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.NoReturnPoint"), textColor);
                return;
            }

            Vector2 standPosition = PrimordialWorldSystem.LandingWorld - new Vector2(player.width / 2f, player.height);
            player.Teleport(standPosition, TeleportationStyleID.DebugTeleport);
            player.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, standPosition.X, standPosition.Y, TeleportationStyleID.DebugTeleport);
            }

            Main.NewText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.Return"), textColor);
        }
    }
}
