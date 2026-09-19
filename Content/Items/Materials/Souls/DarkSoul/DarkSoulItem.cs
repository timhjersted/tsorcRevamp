using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Materials.Souls.DarkSoul
{
    public class DarkSoulItem : BaseRarityItem
    {
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 4));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
            ItemID.Sets.ItemIconPulse[Item.type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
            /* Tooltip.SetDefault("Soul of a fallen creature." +
                "\nCan be used at Demon Altars to forge new weapons, items, and armors."); */
        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = refItem.width;
            Item.height = refItem.height;
            Item.maxStack = 999999;
            Item.value = 1;
            Item.rare = ItemRarityID.Lime;
            DarkSoulRarity = 12;
        }

        public override bool GrabStyle(Player player)
        {
            //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 10f;
            Item.velocity = movement;
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= 2 + Main.LocalPlayer.GetModPlayer<DarkSoulPlayer>().SoulPickupRange;
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.15f, 0.6f, 0.32f);

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

        public override bool OnPickup(Player player)
        {

            SoundStyle PickupSound = SoundID.NPCDeath52;
            PickupSound.Volume = 0.15f;
            PickupSound.PitchVariance = 0.3f;
            SoundEngine.PlaySound(PickupSound, player.position); // Plays sound.

            int quantity = Item.stack / 50;

            if (quantity > 10)
            {
                quantity = 10;
            }

            for (int j = 1; j < 6 + 1 * quantity; j++)
            {
                int z = Dust.NewDust(player.position, player.width, player.height, 89, 0f, 0f, 120, default, 1f);
                Main.dust[z].noGravity = true;
                Main.dust[z].velocity *= 2.75f;
                Main.dust[z].fadeIn = 1.3f;
                Vector2 vectorother = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                vectorother.Normalize();
                vectorother *= Main.rand.Next(60, 100) * 0.04f;
                Main.dust[z].velocity = vectorother;
                vectorother.Normalize();
                vectorother *= 35f;
                Main.dust[z].position = player.Center - vectorother;
            }

            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoulItem>())
            {
                modPlayer.SoulSlot.Item = Item.Clone();
            }
            else
            {
                modPlayer.SoulSlot.Item.stack += Item.stack;
            }

            SoundEngine.PlaySound(SoundID.Grab, new Vector2(player.position.X, player.position.Y));
            PopupText.NewText(PopupTextContext.RegularItemPickup, Item, Item.stack);
            return false;
        }

        //allow picking up even when out of inventory space
        public override bool ItemSpace(Player player)
        {
            return true;
        }
    }
}