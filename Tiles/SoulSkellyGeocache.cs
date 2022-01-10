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
                    Item.NewItem(new Vector2(position.X * 16, position.Y * 16), 16, 16, thisCache.item, thisCache.itemQuantity);
                }
            }
        }


        public static void InitializeSkellys()
        {
            SoulSkellyList = new List<SoulSkellyGeocache>();


            //SOUL SKELLYS - They are 3x1, so use 3, 1 in the rectangle

            //Test skelly placed in Earth Temple
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4221, 951, 3, 1), ModContent.ItemType<Items.Accessories.CosmicWatch>(), 1)); //int x and y of the rectangle are the top left tiles' x and y position
            //SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(0, 0, 3, 1), ModContent.ItemType<Items.Humanity>(), 1));




            //SOUL SKULLS - They are 2x2, so use 2, 2 in the rectangle

            //Blank
            //SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(0, 0, 2, 2), ModContent.ItemType<Items.FadingSoul>(), 1));




            //HANGING SOUL SKELLYS - They are 3x3, so use 3, 3 in the rectangle

            //Blank
            //SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(0, 0, 3, 3), ModContent.ItemType<Items.FadingSoul>(), 1));

        }

    }
}
