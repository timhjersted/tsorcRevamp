using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Stage 2 of Gwyn's Spear of the First Sun: the delayed judgment bolt. Spawned at the thrown
    ///spear's impact, it snaps down to the floor below, marks the spot with a golden rune circle
    ///(GwynStrikeMark.png) that spins and brightens for a ~40t telegraph, then a lightning bolt
    ///crashes down (flash + thunder) and electricity races along the floor both ways
    ///(GwynFloorSpark). Damage only in the brief strike window — the mark itself is a warning.
    ///</summary>
    class GwynLightningStrike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/GwynStrikeMark";

        const int TelegraphTicks = 40;
        const int StrikeTicks = 12;
        const int FloorSparkTiles = 12;

        int Timer => (int)Projectile.localAI[0];
        bool Striking => Timer > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 60;
            Projectile.height = 220; // a tall column at the strike; collides only while striking
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = TelegraphTicks + StrikeTicks;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            //Snap the column's base to the floor under the impact
            float groundY = FindGroundY(Projectile.Center, 20);
            if (groundY > 0f)
            {
                Projectile.position.Y = groundY - Projectile.height;
                Projectile.position.X = Projectile.Center.X - Projectile.width / 2f;
            }
        }

        public override bool? CanDamage()
        {
            return Striking;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float groundY = Projectile.position.Y + Projectile.height;

            if (!Striking)
            {
                //Rune telegraph: rising gold motes through the column + a glint on the ground mark
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-30f, 30f), groundY - Main.rand.NextFloat(Projectile.height));
                    int mote = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -2f, 100, default, 0.9f);
                    Main.dust[mote].noGravity = true;
                }
                Lighting.AddLight(new Vector2(Projectile.Center.X, groundY - 20f), 0.4f, 0.35f, 0.15f);

                if (Timer == TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(new Vector2(Projectile.Center.X, groundY), 5f, 12);
                    //Electricity races along the floor both ways from the strike point
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int dir = -1; dir <= 1; dir += 2)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.Center.X, groundY - 16f), Vector2.Zero,
                                ModContent.ProjectileType<GwynFloorSpark>(), Projectile.damage, 3f, Projectile.owner, dir, FloorSparkTiles);
                        }
                    }
                }
                return;
            }

            //Strike: a blazing lightning column along the full height
            for (int i = 0; i < 12; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-24f, 24f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 40, default, Main.rand.NextFloat(1.5f, 2.3f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-3f, 3f));
            }
            for (int seg = 0; seg < 4; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height * (seg + 0.5f) / 4f), 1f, 0.9f, 0.4f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        ///<summary>The rune circle mark, drawn flat on the ground, spinning + brightening as the
        ///strike nears; hidden once the bolt actually falls (the dust column takes over).</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Striking)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float progress = Timer / (float)TelegraphTicks;
            float groundY = Projectile.position.Y + Projectile.height;
            Vector2 pos = new Vector2(Projectile.Center.X, groundY - 6f) - Main.screenPosition;
            float scale = 0.28f;                       // 408px art → ~7-tile mark
            float alpha = 0.3f + 0.6f * progress;
            Color col = new Color(255, 220, 90) * alpha;
            Main.EntitySpriteDraw(texture, pos, null, col, Main.GlobalTimeWrappedHourly * 2f, texture.Size() / 2f, scale, SpriteEffects.None, 0);
            return false;
        }

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }
    }
}
