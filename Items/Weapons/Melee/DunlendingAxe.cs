using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class DunlendingAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A cruel hill-man's axe fashioned to kill men" +
                 "\nDeals massive damage to humans");
        }
        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.width = 48;
            Item.height = 44;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 11;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 3500;
            Item.rare = ItemRarityID.White;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {

            if (target.type == ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Warlock>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianAmazon>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianValkyrie>()
                || target.type == ModContent.NPCType<NPCs.Enemies.ManHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Necromancer>()
                || target.type == ModContent.NPCType<NPCs.Enemies.RedCloudHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Assassin>()
                )
            {
                damage *= 4; // *2 > *4, lets make it actually useful shall we
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                damage *= 6;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.Dunlending>())
            {
                damage *= 4;
            }
        }

    }
}
