matrix WorldViewProjection;

texture baseNoise;
sampler baseNoiseSampler = sampler_state
{
    Texture = (baseNoise);
    AddressU = wrap;
    AddressV = wrap;
};

texture secondaryNoise;
sampler secondaryNoiseSampler = sampler_state
{
    Texture = (secondaryNoise);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 slashDark;
float4 slashCenter;
float4 slashEdge;
float baseNoiseUOffset;

struct VertexShaderInput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 TextureCoordinates : TEXCOORD0;
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

// Artorias's cached blade arc supplies the parenthesis silhouette. This shader ports Nito's
// opposing-flow death-magic material onto that honest geometry instead of stamping a white sprite.
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    float history = uv.x;
    float across = abs(uv.y - 0.5) * 2.0;
    float2 phase = float2(baseNoiseUOffset * 5.7, baseNoiseUOffset * 2.3);

    float macro = tex2D(baseNoiseSampler,
        float2(history * 1.45 - time * 0.74, uv.y * 2.6) + phase).r;
    float detail = tex2D(secondaryNoiseSampler,
        float2(history * 2.8 - time * 1.18, uv.y * 4.1) - phase).r;
    float churn = saturate(macro * 0.72 + detail * 0.44 - 0.12);

    float tailTaper = saturate(history * 6.25);
    float headTaper = saturate((1.0 - history) * 8.33);
    float profile = tailTaper * headTaper;
    float reach = profile * (0.70 + churn * 0.22);
    float body = saturate((reach - across) * 4.8);
    float erosion = saturate(churn + 0.54 - (1.0 - history) * 0.18);
    body *= erosion;

    float rim = saturate(1.0 - abs(body - 0.28) * 3.25)
        * (0.38 + churn * 0.68) * erosion;
    float leadingHeat = saturate((history - 0.58) * 3.12)
        * saturate((1.0 - history) * 10.0);
    float sparks = saturate(detail * 2.9 - 1.95) * body * (0.32 + leadingHeat * 0.68);

    float opacity = fadeOut * input.Color.a;
    float3 material = slashDark.rgb * (body * 0.72)
        + slashCenter.rgb * (body * (0.42 + churn * 0.48))
        + slashEdge.rgb * ((rim * 0.70 + sparks * 0.42 + leadingHeat * rim * 0.30));
    return float4(material * opacity, saturate(body + rim) * opacity);
}

technique ArtoriasSwordTrail
{
    pass SwordTrailPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
}
