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
float4 shaderColor2;
float length;
float finalStand;

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
    //float pixelSize = 0.0005;
    //uv.x = uv.x - fmod(uv.x, pixelSize);
    //uv.y = uv.y - fmod(uv.y, pixelSize  * 50);
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    //Raise it to a high exponent, resulting in sharply increased intensity at the center that trails off smoothly
    //Higher number = more narrow and compressed trail
    intensity = pow(intensity, 4.0);
    
    //This controls where the front of the bolt starts to curve
    float inflectionPoint = 0.82;
    
    //Don't taper off toward the end if in Final Stand mode
    float start = 0.99;
    float end = 0.05;
    float startFactor = 3;
    float endFactor = 4;
    
    if (finalStand != 0)
    {
        start = 0.99;
        end = 0.01;
        startFactor = 1;
        endFactor = 1;
    }
    
    //Make it fade in towards the start
    if (uv.x > start)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - start), startFactor));
    }
    if (uv.x < end)
    {
        intensity = lerp(0.0, intensity, pow((uv.x) / (end), endFactor));
    }
    
    //Taper off toward the edges
    float yStart = 0.7;
    float yEnd = 1 - yStart;
    if (uv.y > yStart)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.y) / (1 - yStart), 3));
    }
    if (uv.y < yEnd)
    {
        intensity = lerp(0.0, intensity, pow((uv.y) / (yEnd), 3));
    }
    
    
    //Pick where to sample the texture used for the flowing effect
    float2 samplePoint1 = uv * 3;
    
    //Zoom in on the noise texture and scale it with trail length
    samplePoint1.x = (samplePoint1.x) * length / 10000.0;
    
    //Compress it
    samplePoint1.y = (samplePoint1.y / 10);
    
    //shift it over time to make it appear to be flowing
    float2 samplePoint2 = samplePoint1;
    samplePoint1.y -= (time * 0.1);
    samplePoint1.x -= (time * 0.1);
    samplePoint2.y += (time * 0.1);
    samplePoint2.x += (time * 0.101);

    //Get the noise texture at that point
    float sampleIntensity = tex2D(textureSampler, samplePoint1).r;
    float sampleIntensity2 = tex2D(textureSampler, samplePoint2).r;
    
    //Mix it with the laser color
    float4 noiseColor1 = float4(1.0, 1.0, 1.0, 1.0);
    noiseColor1.rgb = sampleIntensity * shaderColor.rgb;
    float4 noiseColor2 = float4(1.0, 1.0, 1.0, 1.0);
    
    if (finalStand == 0)
    {
        noiseColor2.rgb = sampleIntensity2 * shaderColor.rgb;        
    }
    else
    {
        noiseColor2.rgb = sampleIntensity2 * shaderColor2.rgb;        
    }
    
    
    
    return (noiseColor1 + noiseColor2) / 5 * 13.0 * pow(intensity, 1.5) * pow(shaderColor, 2.5) * fadeOut;
}


technique MoltenWeld
{
    pass MoltenWeldPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};