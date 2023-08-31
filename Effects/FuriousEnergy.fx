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
float4 slashCenter;
float4 slashEdge;
float length;
float speed;
float2 samplePointOffset1;
float2 samplePointOffset2;
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
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    float intensity2 = pow(intensity, 6.0);
    
    //Flat doubling to incrase the total intensity
    intensity2 *= 2;
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.82;
    //inflectionPoint *= fadeOut;
    
    //Make it fade out towards the end
    if (uv.x < inflectionPoint)
    {
        intensity2 = lerp(0.0, intensity2, pow(uv.x / inflectionPoint, 1));
    }
    
    //Make it fade in towards the start
    if (uv.x > inflectionPoint)
    {
        intensity2 = lerp(0.0, intensity2, pow((1.0 - uv.x) / (1 - inflectionPoint), 1));
    }

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint = uv;
    
    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing    
    samplePoint /= 20;
    samplePoint.x = (samplePoint.x + time * 0.05) * 1;
    
    //Compress it vertically
    samplePoint.y = samplePoint.y / 2;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(baseNoiseSampler, samplePoint).r;
    
    //Mix it with 2 times the laser color
    float4 noiseColor = 4 * sampleIntensity.rrrr;
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 6.0) * 3 * uv.x;
        
    
    //Make it fade out towards all 4 edges
    float start = 0.8;
    float end = 0.01 + (1 - fadeOut);
    float yStart = 0.9;
    float yEnd = 0.1;
    
    if (uv.x > start)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - start), 2));
    }
    if (uv.x < end)
    {
        intensity = lerp(0.0, intensity, pow((uv.x) / (end), 4));
    }
    if (uv.y > yStart)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.y) / yEnd, 5));
    }
    if (uv.y < yEnd)
    {
        intensity = lerp(0.0, intensity, pow(uv.y / yEnd, 5));
    }
    
    
    //Yet again tune the curve
    intensity = pow(intensity, 1.75);
    
    //Make it fade out toward the far edge
    intensity = intensity * uv.x;
    
    float2 baseNoiseVector = float2((uv.x / 5) + time / 5, uv.y);
    float baseNoiseIntensity = tex2D(baseNoiseSampler, baseNoiseVector).r;
    baseNoiseIntensity = pow(baseNoiseIntensity, 3) * 10;    
    
    intensity *= pow(baseNoiseIntensity, 1);
    
    float4 trailColor = slashCenter;
    if (intensity < 0.9)
    {
        trailColor = slashEdge;
    }
    
    //TODO: Replace this with actual tonemapping
    if (intensity > 1)
    {
        //intensity = 1;
    }
    
    return 1.5 * (trailColor * intensity * noiseColor) * fadeOut * fadeOut;
}

technique FuriousEnergy
{
    pass EffectPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};