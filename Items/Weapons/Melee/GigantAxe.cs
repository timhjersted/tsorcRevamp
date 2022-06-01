using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class GigantAxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("An axe used to kill humans.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 330;
            Item.height = 80;
            Item.knockBack = 9;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .9f;
            Item.useAnimation = Item.useTime = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Cyan_9;
            Item.width = 84;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            //todo add mod NPCs to this list
            if (target.type == ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Warlock>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianAmazon>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianValkyrie>()
                || target.type == ModContent.NPCType<NPCs.Enemies.ManHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Necromancer>()
                || target.type == ModContent.NPCType<NPCs.Enemies.RedCloudHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Assassin>()
                || target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Dunlending>())
            {
                damage *= 2;
            }
        }
    }
}
