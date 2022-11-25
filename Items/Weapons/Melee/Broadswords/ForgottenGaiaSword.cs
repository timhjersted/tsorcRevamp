using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class ForgottenGaiaSword : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A blade made to slay the Witchking.\n" + "[c/ffbf00:Does 3x damage to the Witchking and dispels the defensive shield of the Witchking and Artorias]");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            Item.damage = 185;
            Item.width = 55;
            Item.height = 55;
            Item.knockBack = 8;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Red_10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }



        /*public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FallenStar, 120);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }*/

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>())
            {
                target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.AddNPCBuff, number: target.whoAmI, number2: ModContent.BuffType<Buffs.DispelShadow>(), number3: 36000);
                    ModPacket shadowPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                    shadowPacket.Write((byte)tsorcPacketID.DispelShadow);
                    shadowPacket.Write(target.whoAmI);
                    shadowPacket.Send();
                }
            }
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
        {
            if (target.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>())
            {
                damage *= 3;
            }
        }
    }
}
