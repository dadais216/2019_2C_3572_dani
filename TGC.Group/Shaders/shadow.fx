//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

int type;

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

#define SMAP_SIZE 1024

float time = 0;

float4x4 g_mViewLightProj;

//Output del Vertex Shader
struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 Norm : TEXCOORD1; // Normales
    float3 Pos : TEXCOORD2; // Posicion real 3d
};

//-----------------------------------------------------------------------------
// Vertex Shader que implementa un shadow map
//-----------------------------------------------------------------------------
void VertShadow(float4 Pos : POSITION,
	float3 Normal : NORMAL,
	float2 texCoordIn : TEXCOORD0,
	out float2 texCoord : TEXCOORD0,
	out float4 oPos : POSITION,
	out float Depth : TEXCOORD2)
{

	texCoord = texCoordIn;
	// transformacion estandard
    oPos = mul(Pos, matWorld); // uso el del mesh
    oPos = mul(oPos, g_mViewLightProj); // pero visto desde la pos. de la luz

    Depth = oPos.w; //el shader original hacia z/w, ni idea por que
}

void PixShadow(float2 texCoord : TEXCOORD0,float Depth : TEXCOORD2, out float4 Color : COLOR)
{
	float4 texelColor = tex2D(diffuseMap, texCoord); 
	//por algun motivo el piso levanta una textura cualquiera, no molesta asi que lo dejo
	if (type == 1) {//descartar bordes de arbol
		if (texelColor.a == 0)
			discard;
	if (texelColor.r + texelColor.g + texelColor.b > 2)
			discard;
	}

    Color = Depth / 50000;
}

technique RenderShadow
{
    pass p0
    {
        VertexShader = compile vs_3_0 VertShadow();
        PixelShader = compile ps_3_0 PixShadow();
    }
}
