

/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

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

//Textura para Lightmap
texture texLightMap;
sampler2D lightMap = sampler_state
{
	Texture = (texLightMap);
};

//Material del mesh
float3 materialEmissiveColor; //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)

//Parametros de la Luz
float3 lightColor; //Color RGB de la luz
float4 lightEye;
float4 lightPosition[9]; //Posicion de la luz
float4 eyePosition; //Posicion de la camara
float lightIntensityEye;
float lightIntensity;
float lightAttenuation; //Factor de atenuacion de la luz

/**************************************************************************************/
/* DIFFUSE_MAP */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_DIFFUSE_MAP
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float4 Color : COLOR;
	float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_DIFFUSE_MAP
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
};

//Vertex Shader
VS_OUTPUT_DIFFUSE_MAP vs_DiffuseMap(VS_INPUT_DIFFUSE_MAP input)
{
	VS_OUTPUT_DIFFUSE_MAP output;

	//Proyectar posicion
	output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
	output.Texcoord = input.Texcoord;

	//Posicion pasada a World-Space (necesaria para atenuacion por distancia)
	output.WorldPosition = mul(input.Position, matWorld);

	/* Pasar normal a World-Space
	Solo queremos rotarla, no trasladarla ni escalarla.
	Por eso usamos matInverseTransposeWorld en vez de matWorld */
	output.WorldNormal = mul(input.Normal, matWorld);

	

	return output;
}

//Input del Pixel Shader
struct PS_DIFFUSE_MAP
{
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
};


float4 light(float4 lightPos, float4 lightIntensity_, float4 texelColor,float3 Nn,PS_DIFFUSE_MAP input) {

	//Normalizar vectores
	float3 lightVec = lightPos.xyz - input.WorldPosition;
	float3 viewVector = eyePosition.xyz - input.WorldPosition;
	float3 halfAngleVec = viewVector + lightVec;


	//if (dot(input.WorldNormal, lightVec) < 0)
	//	return float4(0, 0, 0, 0);
	//else {
		float3 Ln = normalize(lightVec);
		float3 Hn = normalize(halfAngleVec);

		//Calcular atenuacion por distancia
		float distAtten = length(lightPos.xyz - input.WorldPosition) * lightAttenuation;

		float4 intensity = lightIntensity_ / distAtten;

		//Componente Ambient
		float3 ambientLight = intensity * lightColor * materialAmbientColor;

		//Componente Diffuse: N dot L
		float3 n_dot_l = dot(Nn, Ln);
		float3 diffuseLight = intensity * lightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l); //Controlamos que no de negativo


		/* Color final: modular (Emissive + Ambient + Diffuse) por el color de la textura, y luego sumar Specular.
		   El color Alpha sale del diffuse material */
		return float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor, 0);
	//}


}

//Pixel Shader
float4 ps_DiffuseMap(PS_DIFFUSE_MAP input) : COLOR0
{

	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);

	if (texelColor.a == 0)
		discard;
	if (texelColor.r+ texelColor.g + texelColor.b >2) //para sacar bordes blancos en los arboles, deberia estar en ellos nomas
		discard;

	float3 Nn = normalize(input.WorldNormal);

	float4 finalColor=float4(0,0,0, materialDiffuseColor.a);
	finalColor+=light(eyePosition, lightIntensityEye, texelColor, Nn, input);

	for (int i = 0; i < 9; i++) {
		finalColor += light(lightPosition[i], lightIntensity, texelColor, Nn, input);
	}
	
	return finalColor;
}

/*
* Technique DIFFUSE_MAP
*/
technique DIFFUSE_MAP
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_DiffuseMap();
		PixelShader = compile ps_3_0 ps_DiffuseMap();
	}
}
