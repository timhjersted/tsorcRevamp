using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;

namespace tsorcRevamp.Tiles
{
    internal class tsorcGlobalTile : GlobalTile
    {
        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            if(type == TileID.Signs)
            {
                Player player = Main.LocalPlayer;
                Vector2 pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
                float distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

                if (!player.dead && distance <= 120f && Main.invasionType == 0)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 30);
                }
            }
        }
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            Player player = Main.LocalPlayer;
            if ((type == TileID.Plants | type == TileID.Plants2) && (player.HasItem(ModContent.ItemType<ToxicShot>()) | player.HasItem(ModContent.ItemType<AlienRifle>()) | player.HasItem(ModContent.ItemType<OmegaSquadRifle>())))
            {
                Item.NewItem(Item.GetSource_NaturalSpawn(), player.Center, ItemID.Seed, 2);
            }
        }
    }
}
