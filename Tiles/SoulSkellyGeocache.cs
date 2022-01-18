using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace tsorcRevamp.Tiles
{
    public class SoulSkellyGeocache //This class stores all Soul Skelly Geocaches, and handles everything necessary for the GiveSoulSkellyLoot method to work
    {                               //To add a new cache, choose a location in the world and place the cache of your choice, take note of its coords in TEdit,
                                    //then add a new list entry in InitializeSkellys()

        static List<SoulSkellyGeocache> SoulSkellyList;

        public Rectangle cacheLocationRect; //this is the location and size of the cache - Rectangle(top left tile positionX, top left tile positionY, width in tiles, height in tiles)
        public int item;
        public int itemQuantity;
        public int itemPrefix;


        public SoulSkellyGeocache(Rectangle locationRect, int givenItem, int givenItemQuantity, int prefix)
        {
            cacheLocationRect = locationRect;
            item = givenItem;
            itemQuantity = givenItemQuantity;
            itemPrefix = prefix;
        }

        public SoulSkellyGeocache(Rectangle locationRect, int givenItem, int givenItemQuantity)
        {
            cacheLocationRect = locationRect;
            item = givenItem;
            itemQuantity = givenItemQuantity;
        }

        public static void GiveSoulSkellyLoot(Vector2 position)
        {
            foreach (SoulSkellyGeocache thisCache in SoulSkellyList)
            {
                if (thisCache.cacheLocationRect.Contains((int)position.X, (int)position.Y))
                {
                    Item.NewItem(new Vector2(position.X * 16, position.Y * 16), 16, 16, thisCache.item, thisCache.itemQuantity, false, thisCache.itemPrefix);
                }
            }
        }


        public static void InitializeSkellys()
        {
            SoulSkellyList = new List<SoulSkellyGeocache>();
            Mod mod = ModContent.GetInstance<tsorcRevamp>();


            //Instructions: Create a new list entry, then create a new instance of SoulSkellyGeocache, which has 3 or 4 parameters: Position (rectangle), Item (int), Quantity (int), and Prefix (int).
            //Position (rectangle): the top left tiles' coordinate (on TEdit) of the cache. Rectangle(top left tile positionX, top left tile positionY, width in tiles, height in tiles).
            //There are 3 sizes of cache - 3x1 (skellys, laying down), 2x2 (skulls) and 3x3 (hanging skellys. Note: Must have walls placed behind them).
            //Item (int): the item we want to give. ItemID.ItemChoice for vanilla items, ModContent.ItemType<Items.etcetc.ItemChoice>() for modded items.
            //Quantity (int): The number of items in the stack.
            //Prefix (int): What prefix we want the item to have. -1 for a random prefix, PrefixID.PrefixChoice for a vanilla prefix, and mod.PrefixType("Blessed") for mod prefixes.
            //Leave blank for items with no prefix.



            //SOUL SKELLYS - They are 3x1, so use 3, 1 in the rectangle. In TEdit, these are GreyStucco-GreenStuccoBlock-GreyStuccoBlock (head on left) and GreenStucco-GreyStuccoBlock-GreenStuccoBlock (head on right)

            //Example: SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4000, 984, 3, 1), ModContent.ItemType<Items.Accessories.CosmicWatch>(), 1)); //int x and y of the rectangle are the top left tiles' x and y position (in the skulls case, the top left air block above the slime blocks)
            //SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(0, 0, 3, 1), ModContent.ItemType<Items.Humanity>(), 1));



            //SOUL SKULLS - They are 2x2, so use 2, 2 in the rectangle. In TEdit, these are SlimeBlock-PinkSlimeBlock(facing left) and PinkSlimeBlock-SlimeBlock(facing right)

            //Earth temple, by the first altar
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4248, 984, 2, 2), ModContent.ItemType<Items.Potions.GreenBlossom>(), 3));

            //Giant Tree, bottom left by roots.
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(2542, 1486, 2, 2), ModContent.ItemType<Items.Accessories.SoulReaper2>(), 1, mod.PrefixType("Revitalizing")));

            //Big Queen Bee Larvae trap room, above the pyramid
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5832, 1531, 2, 2), ModContent.ItemType<Items.Humanity>(), 1));




            //HANGING SOUL SKELLYS - They are 3x3, so use 3, 3 in the rectangle. In TEdit, these are a ConfettiBlock (hanging from wrists) and BlackConfettiBlock, also called MidnightConfettiBlock (hanging from ankles)

            //Blank
            //SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(0, 0, 3, 3), ModContent.ItemType<Items.FadingSoul>(), 1));

        }

    }
}
