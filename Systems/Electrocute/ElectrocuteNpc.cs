using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Electrocute;

public class ElectrocuteNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    
    public int[] ElectrocuteTimer1 = new int[Main.maxPlayers];
    public int[] ElectrocuteTimer2 = new int[Main.maxPlayers];
    public int[] ElectrocuteTimer3 = new int[Main.maxPlayers];

    public override void ResetEffects(NPC npc)
    {
        if (Electrocute.Enabled)
        {
            foreach (var player in Main.ActivePlayers)
            {
                var pT = player.whoAmI;
                var modPlayer = player.GetModPlayer<ElectrocutePlayer>();
                if (!modPlayer.CanElectrocute)
                {
                    return;
                }
                
                if (ElectrocuteTimer1[pT] > 0)
                {
                    ElectrocuteTimer1[pT]++;
                }
                if (ElectrocuteTimer2[pT] > 0)
                {
                    ElectrocuteTimer2[pT]++;
                }
                if (ElectrocuteTimer3[pT] > 0)
                {
                    ElectrocuteTimer3[pT]++;
                }

                if (ElectrocuteTimer1[pT] > ElectrocutePlayer.TimeWindowInSec * 60)
                {
                    modPlayer.ElectrocuteProjectileType1[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileDamage1[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileCritChance1[npc.whoAmI] = 0;
                    ElectrocuteTimer1[pT] = 0;
                }
                if (ElectrocuteTimer2[pT] > ElectrocutePlayer.TimeWindowInSec * 60)
                {
                    modPlayer.ElectrocuteProjectileType2[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileDamage2[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileCritChance2[npc.whoAmI] = 0;
                    ElectrocuteTimer2[pT] = 0;
                }
                if (ElectrocuteTimer3[pT] > ElectrocutePlayer.TimeWindowInSec * 60)
                {
                    modPlayer.ElectrocuteProjectileType3[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileDamage3[npc.whoAmI] = 0;
                    modPlayer.ElectrocuteProjectileCritChance3[npc.whoAmI] = 0;
                    ElectrocuteTimer3[pT] = 0;
                }
            }
        }
    }

    public void ResetElectrocuteFields(NPC npc)
    {
        foreach (var player in Main.ActivePlayers)
        {
            var pT = player.whoAmI;
            var modPlayer = player.GetModPlayer<ElectrocutePlayer>();
            
            ElectrocuteTimer1[pT] = 0;
            ElectrocuteTimer2[pT] = 0;
            ElectrocuteTimer3[pT] = 0;
            modPlayer.ElectrocuteProjectileType1[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileDamage1[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileCritChance1[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileType2[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileDamage2[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileCritChance2[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileType3[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileDamage3[npc.whoAmI] = 0;
            modPlayer.ElectrocuteProjectileCritChance3[npc.whoAmI] = 0;

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket electrocutePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                electrocutePacket.Write(tsorcPacketID.SyncElectrocuteVars);
                electrocutePacket.Write((byte)player.whoAmI);
                electrocutePacket.Write((byte)npc.whoAmI);
                electrocutePacket.Write(ElectrocuteTimer1[pT]);
                electrocutePacket.Write(ElectrocuteTimer2[pT]);
                electrocutePacket.Write(ElectrocuteTimer3[pT]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileType1[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileDamage1[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileCritChance1[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileType2[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileDamage2[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileCritChance2[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileType3[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileDamage3[npc.whoAmI]);
                electrocutePacket.Write(modPlayer.ElectrocuteProjectileCritChance3[npc.whoAmI]);
                electrocutePacket.Send();
            }
        }
    }

    public override void OnKill(NPC npc)
    {
        ResetElectrocuteFields(npc);
    }
}