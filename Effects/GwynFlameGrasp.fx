sampler FlameTexture : register(s0);
sampler SmoothNoise : register(s1);

float3 CinderColor;
float3 FlameColor;
float3 CoreColor;
float Opacity;
float Time;
float2 PrimaryTextureSize;

float4 GwynFlameGraspPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords * PrimaryTextureSize * 0.01724138;
    float2 p = (uv - float2(0.5, 0.5)) * 2.0;
    float flameNoise = tex2D(FlameTexture, uv * 1.7 + float2(-Time * 0.48, Time * 0.14)).r;
    float breakup = flameNoise * 1.25 - 0.15;

    float2 palmPoint = p + float2(0.10, -0.02);
    float palmDistance = dot(palmPoint, palmPoint);
    float palm = saturate((0.30 - palmDistance) * 10.0);
    float fingerStripe = saturate(1.0 - abs(frac((p.y + 0.62) * 4.1) - 0.5) * 3.4);
    float forwardFan = saturate((p.x + 0.05) * 4.0)
        * saturate((0.54 - abs(p.y)) * 8.33);
    float fingers = fingerStripe * forwardFan * (0.54 + breakup * 0.72);

    float hitField = saturate((0.67 - dot(p, p)) * 6.25);
    float hand = max(palm, fingers) * hitField;
    float palmCore = saturate((0.12 - palmDistance) * 10.0);
    float rearWake = saturate((-0.16 - p.x) * 1.351) * breakup;

    float intensity = hand * (0.62 + breakup * 0.72) + palmCore * 1.25 + rearWake * 0.26;
    float3 color = lerp(CinderColor, FlameColor, saturate(hand * 1.1 + breakup * 0.24));
    color += CoreColor * palmCore * 0.65;
    float alpha = saturate(hand + palmCore + rearWake * 0.30) * Opacity;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

technique GwynFlameGrasp
{
    pass GwynFlameGraspPass
    {
        PixelShader = compile ps_2_0 GwynFlameGraspPixel();
    }
}
