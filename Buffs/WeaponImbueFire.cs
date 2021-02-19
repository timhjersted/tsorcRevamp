using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class WeaponImbueFire : GlobalBuff {

        public override void Update(int type, Player player, ref int buffIndex) {
            base.Update(type, player, ref buffIndex);
            player.meleeDamage += 0.1f;
        }

        public override void ModifyBuffTip(int type, ref string tip, ref int rare) {
            tip = "Gives 10% melee damage and melee attacks set enemies on fire";
        }
    }
}
