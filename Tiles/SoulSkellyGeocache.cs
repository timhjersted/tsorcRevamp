using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


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
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4248, 984, 2, 2), ModContent.ItemType<Items.LostUndeadSoul>(), 1));

            //Giant Tree, bottom left by roots.
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(2542, 1486, 2, 2), ModContent.ItemType<Items.Accessories.SoulReaper2>(), 1, mod.PrefixType("Revitalizing")));

            //Big Queen Bee Larvae trap room, above the pyramid
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5832, 1531, 2, 2), ItemID.WaspGun, 1, -1));

            //Corrupt tunnel left of village
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3790, 696, 2, 2), ItemID.Boomstick, 1, PrefixID.Unreal));

            //Tunnel after twin EoW fight
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3931, 1300, 2, 2), ModContent.ItemType<Items.Humanity>(), 3));

            //Jumping puzzle in mountain maze
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3533, 417, 2, 2), ModContent.ItemType<Items.GreatMagicMirror>(), 1));

            //Under the familiar bridge
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4652, 899, 2, 2), ModContent.ItemType<Items.Potions.GreenBlossom>(), 3));

            //Right side of Red Knight event arena on PoP
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3957, 1206, 2, 2), ModContent.ItemType<Items.Potions.RadiantLifegem>(), 3));

            //Very leftmost top of tower in Forgotten City 
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4090, 1580, 2, 2), ModContent.ItemType<Items.ProudKnightSoul>(), 1));

            //Behind safehouse under Forgotten City leading to early Underworld 
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4810, 1819, 2, 2), ModContent.ItemType<Items.Weapons.Melee.RuneBlade>(), 1, PrefixID.Legendary));

            //WoF summoning pedestal
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3397, 1827, 2, 2), ModContent.ItemType<Items.PowerWithin>(), 1));

            //Underworld, in ceiling left of WoF summoning pedestal above lava
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3235, 1839, 2, 2), ItemID.HeartreachPotion, 5));

            //Somewhere in the hallowed Caves
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(7070, 1381, 2, 2), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));




            //HANGING SOUL SKELLYS - They are 3x3, so use 3, 3 in the rectangle. In TEdit, these are a ConfettiBlock (hanging from wrists) and BlackConfettiBlock, also called MidnightConfettiBlock (hanging from ankles)

            //In EoC2 arena, on Path of Pain
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3541, 1242, 3, 3), ModContent.ItemType<Items.StaminaVessel>(), 1));

            //By bonfire leading up to Earth Temple EoC arena, by shortcut
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4170, 1133, 3, 3), ModContent.ItemType<Items.Weapons.Throwing.Firebomb>(), 5));

            //Ceiling of Red Knight event arena on PoP
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3897, 1195, 3, 3), ModContent.ItemType<Items.PurgingStone>(), 1));

            //Entrance of Artorias room, where 2 lothric event spawns
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5168, 1749, 3, 3), ModContent.ItemType<Items.Potions.StrengthPotion>(), 3));

            //Jungle Ruins after getting hook
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5470, 1093, 3, 3), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));
        }
    }
}
