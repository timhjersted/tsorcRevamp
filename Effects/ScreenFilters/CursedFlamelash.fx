matrix WorldViewProjection;
texture noiseTexture;
sampler textureSampler = sampler_state
{
    Texture = (noiseTexture);
    AddressU = wrap;
    AddressV = wrap;
};

float fadeOut;
float time;
float4 shaderColor;
float length;
float speed;
float2 samplePointOffset1;
float2 samplePointOffset2;

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
    //return float4(0.8, 1, 0.4, 1);
    float2 uv = input.TextureCoordinates;
    //float pixelSize = 0.0005;
    //uv.x = uv.x - fmod(uv.x, pixelSize);
    //uv.y = uv.y - fmod(uv.y, pixelSize  * 50);
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 3.0);
    
    //Flat doubling to incrase the total intensity
    intensity *= 1;
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.9;
    //inflectionPoint *= fadeOut;
    
    //Make it fade out towards the end
    if (uv.x < inflectionPoint)
    {
        intensity = lerp(0.0, intensity, pow(uv.x / inflectionPoint, 1));
    }
    
    //Make it fade in towards the start
    if (uv.x > inflectionPoint)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - inflectionPoint), 1));
    }
    
    intensity = pow(intensity, 5.0);
    
    intensity *= fadeOut * fadeOut;

    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint1 = uv;
    
    //Zoom in on the noise texture, then shift it over time to make it appear to be flowing    
    //samplePoint /= 70;
    samplePoint1.x = (samplePoint1.x) * length / 100; // ;
    
    //Compress it vertically
    samplePoint1.y = (samplePoint1.y / 1);
    samplePoint1 /= 4;
    
    float2 samplePoint2 = samplePoint1;
    samplePoint1 += samplePointOffset1;
    samplePoint2 += samplePointOffset2;

    //Get the noise texture at that point
    float sampleIntensity = tex2D(textureSampler, samplePoint1).r;
    sampleIntensity *= tex2D(textureSampler, samplePoint2).r;
    
    //Mix it with the laser color
    float4 noiseColor = 1 - float4(1.0, 1.0, 1.0, 1.0);
    noiseColor.r = sampleIntensity * shaderColor.r;
    noiseColor.b = sampleIntensity * shaderColor.b;
    noiseColor.g = sampleIntensity * shaderColor.g;
    
    //return noiseColor;
    //
    //Mix it with 'intensity' to make it more intense near the center
    //float4 effectColor = pow(noiseColor, 0.85) * pow(intensity, 2) * 1.0;
    
    //Not the vibe i'm going for here, but looks cool as hell and will be useful later:
    if (intensity > 1)
    {
        intensity = 1;
    }
    
    return pow(noiseColor, 1) * 13.0 * intensity * pow(shaderColor, 2.5);
}


technique CursedFlamelash
{
    pass CursedFlamelashPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};