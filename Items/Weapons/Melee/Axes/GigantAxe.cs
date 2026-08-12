using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    class GigantAxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("An axe used to kill humans.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Purple;
            Item.damage = 325;
            Item.height = 80;
            Item.knockBack = 9;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 22;
            Item.useTime = 22;
            Item.scale = 1.5f;
            Item.axe = 150 / 5; // Same as Vortex hamaxe
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Purple_11;
            Item.width = 84;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Gray;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //todo add mod NPCs to this list
            if (tsorcRevamp.HumanNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 1.75f;
            }
        }
    }
}
