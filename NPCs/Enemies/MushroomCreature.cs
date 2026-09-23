using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.NPCs.Enemies
{
    public class MushroomCreature : ModNPC
    {
        //Truffle's sheet ends with 7 "extra" frames (talking, attacking, sitting in a chair). Vanilla only skips
        //those when isLikeATownNPC is true, which this one isn't, so its walk cycle runs straight through them.
        private const int ExtraFrames = 7;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Truffle];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Truffle);

            AIType = NPCID.Truffle;
            AnimationType = NPCID.Truffle;

            NPC.townNPC = false;
            NPC.friendly = false;
            NPC.dontTakeDamage = false;
            NPC.damage = 0;
            NPC.value = 0;
        }

        public override void FindFrame(int frameHeight)
        {
            int lastWalkFrame = Main.npcFrameCount[Type] - ExtraFrames - 1;

            if (NPC.frame.Y / frameHeight > lastWalkFrame)
            {
                NPC.frame.Y = frameHeight * 2; //vanilla restarts the walk cycle on frame 2
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!tsorcRevampWorld.OnlyAdventureMap || tsorcRevampWorld.RemixMap)
            {
                return 0f;
            }

            //Legacy-space spawn box; ExpandedWorldTransform shifts it to match the current world (identity
            //on the non-expanded adventure map, +200 Y on the expanded one - both coords are above the fold).
            Point spawnAreaMin = ExpandedWorldTransform.MapTile(2550, 1300);
            Point spawnAreaMax = ExpandedWorldTransform.MapTile(2850, 1650);

            bool insideSpawnArea = spawnInfo.SpawnTileX >= spawnAreaMin.X && spawnInfo.SpawnTileX <= spawnAreaMax.X
                && spawnInfo.SpawnTileY >= spawnAreaMin.Y && spawnInfo.SpawnTileY <= spawnAreaMax.Y;

            return insideSpawnArea ? 0.5f : 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>()));
        }
    }
}
