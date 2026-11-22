using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items
{
    // Legacy item. Shops now take Dark Souls directly, so a Soul Coin is only ever a stand-in for SoulsPerCoin Dark Souls.
    // The class stays because ~45 loot tables, bags and events still drop it and old saves still hold it:
    // a spawned coin becomes Dark Souls on the spot, and any coin that reaches a player is moved into the soul slot.
    class SoulCoin : BaseRarityItem
    {
        public const int SoulsPerCoin = 5;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulsPerCoin);

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 22;
            Item.maxStack = 99999;
            Item.value = 1;
            Item.rare = ItemRarityID.Lime;
            DarkSoulRarity = 12;
        }

        // Adds coin.stack * SoulsPerCoin Dark Souls to the player's soul slot and empties the coin.
        // Callers: pickup, inventory arrival (e.g. taken from a chest) and the world-entry sweep. Returns the souls added.
        public static int ConvertToSoulSlot(Player player, Item coin)
        {
            int soulsAdded = coin.stack * SoulsPerCoin;
            coin.TurnToAir();

            Item soulSlotItem = player.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
            if (soulSlotItem.IsAir || soulSlotItem.type != ModContent.ItemType<DarkSoulItem>())
            {
                player.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item = new Item(ModContent.ItemType<DarkSoulItem>(), soulsAdded);
            }
            else
            {
                soulSlotItem.stack += soulsAdded;
            }

            return soulsAdded;
        }

        // Every drop path (NPC loot, bags, QuickSpawnItem, events) goes through Item.NewItem, which calls OnSpawn
        // before the server broadcasts the item, so clients only ever receive the Dark Souls.
        public override void OnSpawn(IEntitySource source)
        {
            int soulCount = Item.stack * SoulsPerCoin;
            Item.SetDefaults(ModContent.ItemType<DarkSoulItem>());
            Item.stack = soulCount;
        }

        // Fallback for a coin that was never spawned through NewItem (e.g. sitting in a world chest from an old save).
        public override bool OnPickup(Player player)
        {
            int soulsAdded = ConvertToSoulSlot(player, Item);
            SoundEngine.PlaySound(SoundID.Grab, player.Center);
            PopupText.NewText(PopupTextContext.RegularItemPickup, new Item(ModContent.ItemType<DarkSoulItem>(), soulsAdded), soulsAdded);
            return false;
        }

        // Catches coins that land in the inventory without a pickup, such as ones taken out of a chest.
        public override void UpdateInventory(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            ConvertToSoulSlot(player, Item);
        }

        public override bool GrabStyle(Player player)
        {
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 10f;
            Item.velocity = movement;
            return true;
        }

        // Converted on pickup, so it never needs inventory room.
        public override bool ItemSpace(Player player)
        {
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (2 + player.GetModPlayer<DarkSoulPlayer>().SoulPickupRange / 2);
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> list)
        {
            foreach (TooltipLine line2 in list)
            {
                if (line2.Mod == "Terraria" && line2.Name == "ItemName")
                {
                    line2.OverrideColor = BaseColor.RarityExample;
                }
            }
        }

        int itemframe = 0;
        int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {

            Lighting.AddLight(Item.Center, 0.1f, 0.45f, 0.21f);
            Texture2D texture = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(SoulCoin)) + "_InWorld");
            var myrectangle = texture.Frame(1, 8, 0, itemframe);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, lightColor, 0f, new Vector2(7, 11), Item.scale, SpriteEffects.None, 0.1f);

            itemframeCounter += Main.rand.Next(1, 3);

            if (itemframeCounter >= 15)
            {
                itemframeCounter = 0;
                if (++itemframe >= 7)
                {
                    itemframe = 0;
                }
            }

            return false;
        }
    }
}
