using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy;

namespace tsorcRevamp.Buffs.Armor
{
    /// <summary>Display only: OrnsteinSpellbladePlayer owns the stack count and its timer.</summary>
    public class OrnsteinStatic : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int stacks = Main.LocalPlayer.GetModPlayer<OrnsteinSpellbladePlayer>().StaticStacks;
            float magicBonus = stacks * OrnsteinBody.MagicDamagePerStatic;
            tip = Description.Format(stacks, magicBonus);
        }
    }

    /// <summary>Display only: OrnsteinSpellbladePlayer owns the Charged Edge timer.</summary>
    public class OrnsteinChargedEdge : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
    }
}
