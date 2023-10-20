using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PrimTrailTest : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Test");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.alpha = 100;
            Projectile.timeLeft = 9999999;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            UsefulFunctions.SmoothHoming(Projectile, Main.MouseWorld, 0.5f, 12f, null, false);

            //Dust.NewDustPerfect(Projectile.Center, DustID.ShadowbeamStaff, Vector2.Zero, Scale: 3).noGravity = true;
        }

        float Progress(float progress)
        {
            float num = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, progress, clamped: true);
            num *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, num);
        }

        Color ColorValue(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 3));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            Main.NewText(timeFactor + 1);
            //result = ;
            result.A = 0;

            return result;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BasicEffect effect = new BasicEffect(Main.graphics.GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.FogEnabled = false;
            effect.World = Matrix.CreateTranslation(-new Vector3(Main.screenPosition.X, Main.screenPosition.Y, 0));
            effect.View = Main.GameViewMatrix.TransformationMatrix;
            var viewport = Main.instance.GraphicsDevice.Viewport;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -1, 1);
            Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            effect.CurrentTechnique.Passes[0].Apply();

            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(Projectile.oldPos, Projectile.oldRot, ColorValue, Progress, includeBacksides: true);
            vertexStrip.DrawTrail();
            effect.Dispose();
            return false;
        }
    }

    public class TestVertexStrip
    {
        public delegate Color StripColorFunction(float progressOnStrip);

        public delegate float StripHalfWidthFunction(float progressOnStrip);

        public struct CustomVertexInfo : IVertexType
        {
            public Vector2 Position;

            public Color Color;

            public Vector2 TexCoord;

            public static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0), new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

            public VertexDeclaration VertexDeclaration => _vertexDeclaration;

            public CustomVertexInfo(Vector2 position, Color color, Vector2 texCoord)
            {
                Position = position;
                Color = color;
                TexCoord = texCoord;
            }
        }

        public CustomVertexInfo[] _vertices = new CustomVertexInfo[1];

        public int _vertexAmountCurrentlyMaintained;

        public short[] _indices = new short[1];

        public int _indicesAmountCurrentlyMaintained;

        public List<Vector2> _temporaryPositionsCache = new List<Vector2>();

        public List<float> _temporaryRotationsCache = new List<float>();

        public void PrepareStrip(Vector2[] positions, float[] rotations, StripColorFunction colorFunction, StripHalfWidthFunction widthFunction, Vector2 offsetForAllPositions = default(Vector2), int? expectedVertexPairsAmount = null, bool includeBacksides = false)
        {
            int positionCount = positions.Length;
            int vertexCount = (_vertexAmountCurrentlyMaintained = positionCount * 2);

            if (_vertices.Length < vertexCount)
            {
                Array.Resize(ref _vertices, vertexCount);
            }

            int expectedVertexPairs = positionCount;
            if (expectedVertexPairsAmount.HasValue)
            {
                expectedVertexPairs = expectedVertexPairsAmount.Value;
            }

            for (int i = 0; i < positionCount; i++)
            {

                //Stop if we hit a position with no data
                if (positions[i] == Vector2.Zero)
                {
                    positionCount = i - 1;
                    _vertexAmountCurrentlyMaintained = positionCount * 2;
                    break;
                }

                //Offset the positions
                Vector2 pos = positions[i] + offsetForAllPositions;
                float rot = MathHelper.WrapAngle(rotations[i]);
                int indexOnVertexArray = i * 2;
                float progressOnStrip = (float)i / (float)(expectedVertexPairs - 1);
                AddVertex(colorFunction, widthFunction, pos, rot, indexOnVertexArray, progressOnStrip);
            }

            PrepareIndices(positionCount, includeBacksides);
        }

        public void PrepareStripWithProceduralPadding(Vector2[] positions, float[] rotations, StripColorFunction colorFunction, StripHalfWidthFunction widthFunction, Vector2 offsetForAllPositions = default(Vector2), bool includeBacksides = false, bool tryStoppingOddBug = true)
        {
            int num = positions.Length;
            _temporaryPositionsCache.Clear();
            _temporaryRotationsCache.Clear();
            for (int i = 0; i < num && !(positions[i] == Vector2.Zero); i++)
            {
                Vector2 vector = positions[i];
                float num2 = MathHelper.WrapAngle(rotations[i]);
                _temporaryPositionsCache.Add(vector);
                _temporaryRotationsCache.Add(num2);
                if (i + 1 >= num || !(positions[i + 1] != Vector2.Zero))
                {
                    continue;
                }

                Vector2 vector2 = positions[i + 1];
                float num3 = MathHelper.WrapAngle(rotations[i + 1]);
                int num4 = (int)(Math.Abs(MathHelper.WrapAngle(num3 - num2)) / (MathF.PI / 12f));
                if (num4 != 0)
                {
                    float num5 = vector.Distance(vector2);
                    Vector2 value = vector + num2.ToRotationVector2() * num5;
                    Vector2 value2 = vector2 + num3.ToRotationVector2() * (0f - num5);
                    int num6 = num4 + 2;
                    float num7 = 1f / (float)num6;
                    Vector2 target = vector;
                    for (float num8 = num7; num8 < 1f; num8 += num7)
                    {
                        Vector2 vector3 = Vector2.CatmullRom(value, vector, vector2, value2, num8);
                        float item = MathHelper.WrapAngle(vector3.DirectionTo(target).ToRotation());
                        _temporaryPositionsCache.Add(vector3);
                        _temporaryRotationsCache.Add(item);
                        target = vector3;
                    }
                }
            }

            int count = _temporaryPositionsCache.Count;
            Vector2 zero = Vector2.Zero;
            for (int j = 0; j < count && (!tryStoppingOddBug || !(_temporaryPositionsCache[j] == zero)); j++)
            {
                Vector2 pos = _temporaryPositionsCache[j] + offsetForAllPositions;
                float rot = _temporaryRotationsCache[j];
                int indexOnVertexArray = j * 2;
                float progressOnStrip = (float)j / (float)(count - 1);
                AddVertex(colorFunction, widthFunction, pos, rot, indexOnVertexArray, progressOnStrip);
            }

            _vertexAmountCurrentlyMaintained = count * 2;
            PrepareIndices(count, includeBacksides);
        }

        public void PrepareIndices(int vertexPaidsAdded, bool includeBacksides)
        {
            int num = vertexPaidsAdded - 1;
            int num2 = 6 + includeBacksides.ToInt() * 6;
            int num3 = (_indicesAmountCurrentlyMaintained = num * num2);
            if (_indices.Length < num3)
            {
                Array.Resize(ref _indices, num3);
            }

            for (short num4 = 0; num4 < num; num4 = (short)(num4 + 1))
            {
                short num5 = (short)(num4 * num2);
                int num6 = num4 * 2;
                _indices[num5] = (short)num6;
                _indices[num5 + 1] = (short)(num6 + 1);
                _indices[num5 + 2] = (short)(num6 + 2);
                _indices[num5 + 3] = (short)(num6 + 2);
                _indices[num5 + 4] = (short)(num6 + 1);
                _indices[num5 + 5] = (short)(num6 + 3);
                if (includeBacksides)
                {
                    _indices[num5 + 6] = (short)(num6 + 2);
                    _indices[num5 + 7] = (short)(num6 + 1);
                    _indices[num5 + 8] = (short)num6;
                    _indices[num5 + 9] = (short)(num6 + 2);
                    _indices[num5 + 10] = (short)(num6 + 3);
                    _indices[num5 + 11] = (short)(num6 + 1);
                }
            }
        }

        public void AddVertex(StripColorFunction colorFunction, StripHalfWidthFunction widthFunction, Vector2 pos, float rot, int indexOnVertexArray, float progressOnStrip)
        {
            while (indexOnVertexArray + 1 >= _vertices.Length)
            {
                Array.Resize(ref _vertices, _vertices.Length * 2);
            }

            Color color = colorFunction(progressOnStrip);
            float width = widthFunction(progressOnStrip);
            Vector2 vector = MathHelper.WrapAngle(rot - MathF.PI / 2f).ToRotationVector2() * width;
            _vertices[indexOnVertexArray].Position = pos + vector;
            _vertices[indexOnVertexArray + 1].Position = pos - vector;
            _vertices[indexOnVertexArray].TexCoord = new Vector2(progressOnStrip, 1f);
            _vertices[indexOnVertexArray + 1].TexCoord = new Vector2(progressOnStrip, 0f);
            _vertices[indexOnVertexArray].Color = color;
            _vertices[indexOnVertexArray + 1].Color = color;
        }

        public void DrawTrail()
        {
            if (_vertexAmountCurrentlyMaintained >= 3)
            {
                Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertexAmountCurrentlyMaintained, _indices, 0, _indicesAmountCurrentlyMaintained / 3);
            }
        }
    }


    /*//MiscShaderData ShaderData = GameShaders.Misc["RainbowRod"];
            //ShaderData.UseSaturation(-.8f);
            //ShaderData.UseOpacity(1f);
            //ShaderData.Apply();
            TestVertexStrip vertexStrip = new TestVertexStrip();
            vertexStrip.PrepareStrip(Projectile.oldPos, Projectile.oldRot, ColorValue, Progress, Projectile.Size / 2f - Main.screenPosition, null, true);

            


                /*
            //vertexStrip.DrawTrail(); 
            //Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            //Main.graphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, 0, 20);

            /*private static VertexStrip _vertexStrip = new VertexStrip();   
              public void Draw(Projectile proj) { 
                  MiscShaderData miscShaderData = GameShaders.Misc["RainbowRod"]; 
                  miscShaderData.UseSaturation(-2.8f); 
                  miscShaderData.UseOpacity(4f); 
                  miscShaderData.Apply(); 
                  _vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColors, StripWidth, -Main.screenPosition + proj.Size / 2f); 
                  _vertexStrip.DrawTrail(); 
                  Main.pixelShader.CurrentTechnique.Passes[0].Apply(); 
             }*/

}
