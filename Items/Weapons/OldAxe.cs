using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class OldAxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Does random damage from 0 to 12" +
                                "\nMaximum damage is increased by damage modifiers.");
        }

        public override void SetDefaults()
        {
            item.damage = 8;
            item.width = 36;
            item.height = 30;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 16;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 9000;
        }

        public override void ModifyHitNPC(Player myPlayer, NPC npc, ref int damage, ref float knockback, ref bool crit)
        {
            damage = (int)((Main.rand.Next(13)) * (myPlayer.meleeDamage));
        }
    }
}
