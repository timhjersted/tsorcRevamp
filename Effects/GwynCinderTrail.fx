sampler PrimaryTexture : register(s0);
sampler FlowNoise : register(s1);

float3 CinderColor;
float3 FlameColor;
float3 CoreColor;
float Opacity;
float Time;
float2 DrawSize;
float2 PrimaryTextureSize;
float Progress;

float2 NormalizedCoordinates(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 GwynCinderArcPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = NormalizedCoordinates(coords);
    float2 p = (uv - float2(0.5, 0.5)) * 2.0;
    float noise = tex2D(PrimaryTexture, uv * 1.85 + float2(-Time * 0.52, Time * 0.16)).r;

    float2 crescentPoint = float2(p.x * 0.92 + 0.14, p.y);
    float shellDistance = abs(dot(crescentPoint, crescentPoint) - 0.397);
    float shell = saturate((0.214 - shellDistance) * 4.67);
    float forwardMask = saturate((p.x + 0.48) * 1.5625);
    float body = shell * forwardMask;
    float core = body * body;
    float intensity = body * (0.42 + noise * 0.88) + core * 0.85;

    float3 color = lerp(CinderColor, FlameColor, saturate(noise * 1.15));
    color += CoreColor * core * 0.55;
    float ageFade = 1.0 - Progress * 0.40;
    float alpha = saturate(body + core * 0.60) * Opacity * ageFade;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

float4 GwynCinderBladePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 sprite = tex2D(PrimaryTexture, coords);
    float2 texel = 1.0 / max(PrimaryTextureSize, float2(1.0, 1.0));
    float neighborAlpha = min(
        min(tex2D(PrimaryTexture, coords + float2(texel.x, 0.0)).a,
            tex2D(PrimaryTexture, coords - float2(texel.x, 0.0)).a),
        min(tex2D(PrimaryTexture, coords + float2(0.0, texel.y)).a,
            tex2D(PrimaryTexture, coords - float2(0.0, texel.y)).a));
    float edge = saturate((sprite.a - neighborAlpha) * 4.0);
    float flow = tex2D(FlowNoise, coords * 2.2 + float2(-Time * 0.48, Time * 0.31)).r;
    float luminance = dot(sprite.rgb, float3(0.299, 0.587, 0.114));
    float heat = sprite.a * (0.42 + flow * 0.62 + luminance * 0.22) + edge * 1.15;

    float3 color = lerp(CinderColor, FlameColor, saturate(flow * 0.92 + luminance * 0.28));
    color = lerp(color, CoreColor, edge);
    float alpha = sprite.a * saturate(0.52 + flow * 0.62 + edge) * Opacity;
    return float4(sampleColor.rgb * color * heat, sampleColor.a * alpha);
}

technique GwynCinderArc
{
    pass GwynCinderArcPass
    {
        PixelShader = compile ps_2_0 GwynCinderArcPixel();
    }
}

technique GwynCinderBlade
{
    pass GwynCinderBladePass
    {
        PixelShader = compile ps_2_0 GwynCinderBladePixel();
    }
}
