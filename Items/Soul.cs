using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using System.Reflection;

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
            item.rare = ItemRarityID.Lime;
        }

        public override bool GrabStyle(Player player) { //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 0.75f;
            item.velocity = item.velocity + movement;
            return true;
        }

        public override void GrabRange(Player player, ref int grabRange) {
            grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper);
        }
    }

    public class DarkSoul : Soul {

        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Tooltip.SetDefault("Soul of a fallen creature." + 
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
            Tooltip.SetDefault("The essence of Attraidies' power burns within this soul." +
                "\nYou question whether you should even hold such a thing in your possession.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.93f, 0.1f, 0.45f);
        }
    }

    public class  SoulOfArtorias : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Soul of Artorias");
            Tooltip.SetDefault("The essence of Artorias of the Abyss.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.9f, 0.9f, 0.9f);
        }

    }
    public class SoulOfBlight : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Soul of Blight");
            Tooltip.SetDefault("The essence of destruction.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.9f, 0.9f, 0.9f);
        }

    }

    public class SoulOfChaos : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Soul of Chaos");
            Tooltip.SetDefault("The essence of chaos.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.70f, 0.20f, 0.13f);
        }

    }

    public class BequeathedSoul : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Bequeathed Lord Soul Shard");
            Tooltip.SetDefault("Soul of the albino Seath the Scaleless." +
                "\nA fragment of a Lord Soul discovered at the dawn of the Age of Fire." +
                "\nSeath allied with Lord Gwyn and turned upon the dragons, and for this he was" + 
                "\nawarded dukedom, embraced by the royalty, and given a fragment of a great soul.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.33f, 0.75f, 0.70f);
        }

    }

    public class GhostWyvernSoul : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Soul of the Ghost Wyvern");
            Tooltip.SetDefault("The essence of the Ghost Wyvern.");
        }

        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.28f, 0.33f, 0.75f);
        }

    }

    public class CursedSoul : Soul {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
            Tooltip.SetDefault("Soul of a cursed being.");
        }
        public override void PostUpdate() {
            Lighting.AddLight(item.Center, 0.85f, 0f, 0f);
        }
    }

}
