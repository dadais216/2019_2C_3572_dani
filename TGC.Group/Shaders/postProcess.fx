
float screen_dx;
float screen_dy;
float pixels;

texture renderTarget;
sampler screenSample =
sampler_state
{
    Texture = <renderTarget>;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

void VSCopy(
	float4 vPos : POSITION, 
	float2 vTex : TEXCOORD0, 
	out float4 oPos : POSITION, 
	out float2 oScreenPos : TEXCOORD0)
{
    oPos = vPos;
    oScreenPos = vTex;
    oPos.w = 1;
}

float4 PSPostProcess(float2 tex : TEXCOORD0
) : COLOR0
{
	tex = floor(tex * pixels) / pixels;
    float4 ColorBase = tex2D(screenSample, tex);

    return ColorBase;
}

technique PostProcess
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 VSCopy();
        PixelShader = compile ps_3_0 PSPostProcess();
    }
}
