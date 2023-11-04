using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Tools;


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
                    for (int i = 0; i < Main.CurrentFrameFlags.ActivePlayersCount; i++)
                    {
                        int index = Item.NewItem(new Terraria.DataStructures.EntitySource_Misc("Soul Skelly"), new Vector2(position.X * 16, position.Y * 16), 16, 16, thisCache.item, thisCache.itemQuantity, false, thisCache.itemPrefix);
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.SyncItem, number: index, number2: 1f);
                        }
                    }
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

            //In Village Mountain in cave accessible from rope vine jumping puzzle
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3689, 414, 2, 2), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));

            //Earth temple, by the first altar
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4248, 984, 2, 2), ModContent.ItemType<GreatMagicMirror>(), 1));

            //Giant Tree, bottom left by roots.
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(2542, 1486, 2, 2), ModContent.ItemType<Items.ProudKnightSoul>(), 3));

            //Big Queen Bee Larvae trap room, above the pyramid
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5832, 1531, 2, 2), ItemID.WaspGun, 1, -1));

            //Corrupt tunnel left of village
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3790, 696, 2, 2), ItemID.Boomstick, 1, PrefixID.Unreal));

            //Tunnel after twin EoW fight
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3931, 1300, 2, 2), ModContent.ItemType<Items.Humanity>(), 3));

            //Jumping puzzle in mountain maze
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3533, 417, 2, 2), ModContent.ItemType<GreatMagicMirror>(), 1));

            //Under the familiar bridge
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4652, 899, 2, 2), ModContent.ItemType<Items.Potions.GreenBlossom>(), 3));

            //Right side of Red Knight event arena on PoP
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3957, 1206, 2, 2), ModContent.ItemType<Items.Potions.RadiantLifegem>(), 3));

            //Very leftmost top of tower in Forgotten City 
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4090, 1580, 2, 2), ModContent.ItemType<Items.ProudKnightSoul>(), 1));

            //Behind safehouse under Forgotten City leading to early Underworld 
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4810, 1819, 2, 2), ModContent.ItemType<Items.Weapons.Melee.Broadswords.RuneBlade>(), 1, PrefixID.Legendary));

            //WoF summoning pedestal
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3397, 1827, 2, 2), ModContent.ItemType<PowerWithin>(), 1));

            //Underworld, in ceiling left of WoF summoning pedestal above lava
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3235, 1839, 2, 2), ItemID.HeartreachPotion, 9));

            //Somewhere in the hallowed Caves
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(7070, 1383, 2, 2), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));




            //HANGING SOUL SKELLYS - They are 3x3, so use 3, 3 in the rectangle. In TEdit, these are a ConfettiBlock (hanging from wrists) and BlackConfettiBlock, also called MidnightConfettiBlock (hanging from ankles)

            // Catacombs of the Drowned (below Ashenpeak)
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4233, 785, 3, 3), ModContent.ItemType<Items.Weapons.Magic.FarronDart>(), 1));

            //Near end of Path of Ambition at the top of the parkour section
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3467, 944, 3, 3), ModContent.ItemType<Items.NamelessSoldierSoul>(), 2));

            //Above FireLurker arena, in hanging prison cell with chest in Path of Ambition
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3585, 1209, 3, 3), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));

            //In FireLurker arena (in left cave beside main big room), on Path of Ambition
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3541, 1242, 3, 3), ModContent.ItemType<Items.StaminaVessel>(), 1));

            //By bonfire leading up to Earth Temple EoC arena, by shortcut
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(4170, 1133, 3, 3), ModContent.ItemType<Items.Weapons.Throwing.Firebomb>(), 7));

            //Ceiling of Red Knight event arena in Path of Ambition/Pain
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(3897, 1195, 3, 3), ModContent.ItemType<Items.PurgingStone>(), 1));

            //Entrance of Artorias room, where 2 lothric event spawns
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5168, 1749, 3, 3), ModContent.ItemType<Items.Potions.StrengthPotion>(), 3));

            //Jungle Ruins after getting hook
            SoulSkellyList.Add(new SoulSkellyGeocache(new Rectangle(5470, 1093, 3, 3), ModContent.ItemType<Items.NamelessSoldierSoul>(), 1));
        }
    }
}
