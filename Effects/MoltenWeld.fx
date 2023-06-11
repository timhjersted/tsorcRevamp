matrix WorldViewProjection;
float4 effectColor;
float time;

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
    // Normalize pixel coordinates (from 0 to 1) and compensate for projectile size distortion
    float2 uv = input.TextureCoordinates;
    
    //Calculate how close the current pixel is to the center line of the screen
    float intensity = 1.0 - abs(uv.y - 0.5);
    
    //Raise it to the power of 4, resulting in sharply increased intensity at the center that trails off smoothly
    intensity = pow(intensity, 5.0);

    //Scale the start and end based on the scaleDown variable
    float startPercent = 0.2;
    float endPercent = 0.95;        

    //Make the laser trail off at the start
    if (uv.x < startPercent)
    {
        intensity = lerp(0.0, intensity, pow(uv.x / startPercent, 0.5));
    }
    
    //Make the laser trail off at the end
    if (uv.x > endPercent)
    {
        intensity = lerp(0.0, intensity, pow((1.0 - uv.x) / (1 - endPercent), 0.5));
    }
    
    return effectColor * intensity;    
}


technique MoltenWeld
{
    pass MoltenWeldPass
    {
        VertexShader = compile vs_2_0 MainVS();
        PixelShader = compile ps_2_0 MainPS();
    }
};