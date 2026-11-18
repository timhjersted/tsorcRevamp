using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.Primordial;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.BossItems
{
    /// <summary>
    /// Dropped by Gwyn. The first use anywhere returns the world to Super Hardmode for good, without the
    /// Abyss. Prime Gwyn is summoned at Gwyn's event location until his own arena and teleport exist.
    /// Multiplayer: the using client asks the server, which validates the use and makes every change.
    /// </summary>
    class EmberOfTheFirstDawn : ModItem
    {
        // Legacy tile coordinates of Gwyn's scripted events; MapTile converts them on the expanded map.
        static readonly Vector2 AdventureSummonTile = new Vector2(832, 1244);
        static readonly Vector2 RemixSummonTile = new Vector2(822, 1241);
        const float SummonUseRange = 100f * 16f; // keeps the user inside Gwyn's 2000px coward ring on arrival
        const float SandboxSummonDistance = 480f;
        const int UseDustCount = 36;

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
            return gwynSlain && !tsorcRevampWorld.BossAlive;
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

            bool canSummonHere = TryGetSummonPoint(player, out _);
            if (!canSummonHere)
            {
                Main.NewText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.TooFar"), new Color(255, 144, 64));
            }

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

        /// <summary>Server or single player. The world change comes first, so it stands even if the
        /// summon cannot happen here or the player later dies to him.</summary>
        internal static void ResolveUse(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !player.active || player.dead)
            {
                return;
            }
            if (!tsorcRevampWorld.BossDefeated(ModContent.NPCType<Gwyn>()))
            {
                return;
            }

            tsorcRevampWorld.EnterPrimordialAge();

            if (tsorcRevampWorld.BossAlive || !TryGetSummonPoint(player, out Vector2 summonBottom))
            {
                return;
            }

            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.EmberOfTheFirstDawn.Summon"), 255, 190, 90);
            NPC.NewNPC(new EntitySource_BossSpawn(player), (int)summonBottom.X, (int)summonBottom.Y,
                ModContent.NPCType<PrimeGwyn>(), 0, 0f, 0f, 0f, 0f, player.whoAmI);
        }

        /// <summary>World point for his feet. Adventure maps use Gwyn's event location and need the user
        /// nearby; sandbox worlds have no such place, so he appears a short way ahead of the user.</summary>
        static bool TryGetSummonPoint(Player player, out Vector2 summonBottom)
        {
            bool hasEventLocation = tsorcRevampWorld.OnlyAdventureMap || tsorcRevampWorld.RemixMap;
            if (!hasEventLocation)
            {
                summonBottom = new Vector2(player.Center.X + player.direction * SandboxSummonDistance, player.Bottom.Y);
                return true;
            }

            Vector2 legacyTile = AdventureSummonTile;
            if (tsorcRevampWorld.RemixMap)
            {
                legacyTile = RemixSummonTile;
            }

            summonBottom = ExpandedWorldTransform.MapTile(legacyTile) * 16f;
            return Vector2.Distance(player.Center, summonBottom) <= SummonUseRange;
        }
    }
}
