using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class BarrowBlade : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Wrought with spells of a fierce power." +
                                "\nDispels the defensive shields of Artorias and the Witchking");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest; //so people know it's important
            Item.damage = 26;
            Item.height = 32;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .9f;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useTime = 21;
            Item.value = PriceByRarity.Blue_1;
            Item.width = 32;
            Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.BarrowBladeProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
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

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit)
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
}
