using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

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
            if (!tsorcRevamp.isAdventureMap || tsorcRevampWorld.RemixMap)
            {
                return 0f;
            }

            bool insideSpawnArea = spawnInfo.SpawnTileX >= 2550 && spawnInfo.SpawnTileX <= 2850
                && spawnInfo.SpawnTileY >= 1300 && spawnInfo.SpawnTileY <= 1650;

            return insideSpawnArea ? 0.5f : 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>()));
        }
    }
}
