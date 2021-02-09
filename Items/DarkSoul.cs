using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace tsorcRevamp.Items {

    public abstract class Soul : ModItem {
        //theres too many souls in this mod, im not making individual files for all of them 
        public override void SetStaticDefaults() {
            Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(5, 4));
            ItemID.Sets.AnimatesAsSoul[item.type] = true;
            ItemID.Sets.ItemIconPulse[item.type] = true;
            ItemID.Sets.ItemNoGravity[item.type] = true;
        }

        public override void SetDefaults() {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            item.width = refItem.width;
            item.height = refItem.height;
            item.maxStack = 999999;
            item.value = 1;
            item.rare = ItemRarityID.Pink;
        }

        public override bool GrabStyle(Player player) { //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default(Vector2)) * 0.75f;
            item.velocity = item.velocity + movement;
            item.velocity = Collision.TileCollision(item.position, item.velocity, item.width, item.height);
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange) { //TODO: adjustable grab range for soul ring accessory
            grabRange *= 4;
        }
    }

    public class DarkSoul : Soul {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Tooltip.SetDefault("Soul of a fallen creature" + 
                "\nCan be used at Demon Altars to forge new weapons, items, and armors.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.15f, 0.72f, 0.05f);
        }
    }
    
    public class GuardianSoul : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Tooltip.SetDefault("Soul of an ancient guardian." + 
                "\nCan be used at Demon Altars to forge powerful weapons and items.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.93f, 0.1f, 0.45f);
        }
    }
    public class SoulOfAttraidies : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Soul of Attraidies");
            Tooltip.SetDefault("The essence of Attraidies' power burns within this soul" +
                "\nYou question whether you should even hold such a thing in your possession.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.93f, 0.1f, 0.45f);
        }
    }
}
