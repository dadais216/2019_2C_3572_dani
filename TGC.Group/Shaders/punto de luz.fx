

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

//Material del mesh
float3 materialEmissiveColor; //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)

//Parametros de la Luz
float3 lightColor; //Color RGB de la luz
float4 lightPosition[9]; //Posicion de la luz
float lightIntensity[9];
float4 eyePosition; //Posicion de la camara
float lightIntensityEye;
float lightAttenuation; //Factor de atenuacion de la luz

int type;
//0 normal
//1 arbol
//2 esqueleto

/**************************************************************************************/
/* DIFFUSE_MAP */
/**************************************************************************************/

//Vertex Shader
void vs_DiffuseMap(
	float4 Position : POSITION0,
	float3 Normal : NORMAL0,
	float2 Texcoord : TEXCOORD0,

	out float4 oPosition : POSITION0,
	out float2 oTexcoord : TEXCOORD0,
	out float3 oWorldPosition : TEXCOORD1,
	out float3 oWorldNormal : TEXCOORD2
)
{
	oPosition = mul(Position, matWorldViewProj);

	oTexcoord = Texcoord;

	oWorldPosition = mul(Position, matWorld);

	oWorldNormal = mul(Normal, matInverseTransposeWorld);
}

float4 light(float4 lightPos, float4 lightIntensity_, float4 texelColor,float3 Nn, float3 WorldPosition) {

	//Normalizar vectores
	float3 lightVec = lightPos.xyz - WorldPosition;
	float3 viewVector = eyePosition.xyz - WorldPosition;
	float3 halfAngleVec = viewVector + lightVec;


	//if (dot(normalize(WorldNormal), normalize(lightVec)) < 0)
	//	return float4(0, 0, 0, 0);
	//else {
		float3 Ln = normalize(lightVec);
		float3 Hn = normalize(halfAngleVec);

		//Calcular atenuacion por distancia
		float distAtten = length(lightPos.xyz - WorldPosition) * lightAttenuation;

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
float4 ps_DiffuseMap(
	float2 Texcoord : TEXCOORD0,
	float3 WorldPosition : TEXCOORD1,
	float3 WorldNormal : TEXCOORD2
) : COLOR0
{

	//Obtener texel de la textura
	float4 texelColor = tex2D(diffuseMap, Texcoord);
	if (type == 1) {//descartar bordes de arbol
		if (texelColor.a == 0)
			discard;
		if (texelColor.r + texelColor.g + texelColor.b > 2)
			discard;
	}
	else if (type == 2) { //esqueleto
		if (texelColor.a == 0)
			discard;
		if (texelColor.r + texelColor.g + texelColor.b < .8)
			discard;
	}

	float3 Nn = normalize(WorldNormal);

	float4 finalColor=float4(0,0,0, materialDiffuseColor.a);
	finalColor+=light(eyePosition, lightIntensityEye, texelColor, Nn, WorldPosition);

	for (int i = 0; i < 9; i++) {
		finalColor += light(lightPosition[i], lightIntensity[i], texelColor, Nn, WorldPosition);
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






float4x4 mViewLightProj;
float3 lightPos;
float3 lightDir;
texture shadowTexture;
sampler2D shadowSampler =
sampler_state
{
	Texture = <shadowTexture>;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};
bool inView;

//-----------------------------------------------------------------------------
// Vertex Shader para dibujar la escena pp dicha con sombras
//-----------------------------------------------------------------------------
void vs_diffuseWShadow(
	float4 iPos : POSITION,
	float3 iNormal : NORMAL,
	float2 iTex : TEXCOORD0,

	out float4 oPos : POSITION,
	out float2 oTex : TEXCOORD0,
	out float3 oWorldPos : TEXCOORD1,
	out float3 oWorldN : TEXCOORD2,
	out float4 oPosFromLight : TEXCOORD3
)
{
	vs_DiffuseMap(
		iPos, iNormal, iTex,
		oPos, oTex, oWorldPos, oWorldN);

	oPosFromLight = mul(mul(iPos,matWorld), mViewLightProj);
}

//-----------------------------------------------------------------------------
// Pixel Shader para dibujar la escena
//-----------------------------------------------------------------------------
float4 ps_diffuseWShadow(
	float2 tex : TEXCOORD0,
	float3 worldPos : TEXCOORD1,
	float3 normal : TEXCOORD2,
	float4 posFromLight : TEXCOORD3
) : COLOR
{
	/*
	float4 color=ps_DiffuseMap(tex,pos,normal);

	float c = tex2D(shadowSampler,tex);

	color.r += c * (1-color.r);
	color.g += c * (1 - color.g);
	color.b += c * (1 - color.b);

	return color;
	*/

	float3 lightL = normalize(float3(worldPos - lightPos));
	float cono = dot(lightL, lightDir);

	float4 K = 0.0;
	/*
	float2 CT = 0.5 * posFromLight.xy / posFromLight.w + float2(0.5, 0.5);
	CT.y = 1.0f - CT.y;
	float val = tex2D(shadowSampler, CT);

	return float4(val,val,val,1);
	*/

	float limit = 0.7;
	if (cono > limit)
	{
		// coordenada de textura CT
		float2 CT = 0.5 * posFromLight.xy / posFromLight.w + float2(0.5, 0.5);
		CT.y = 1.0f - CT.y;

		float I = (tex2D(shadowSampler, CT) + 0.010 > posFromLight.w / 50000) ? 1.0f : 0.0f;

		// 1-limit    ____ 1 
		// cono-limit ____ x
		K = I * (cono-limit)/(1-limit);
	}

	float4 color_base = ps_DiffuseMap(tex, worldPos, normal);
	if(inView)
		color_base.r += K;
	else
		color_base.b += K;
	return color_base;
}

technique DIFFUSEWITHSHADOW
{
	pass p0
	{
		VertexShader = compile vs_3_0 vs_diffuseWShadow();
		PixelShader = compile ps_3_0 ps_diffuseWShadow();
	}
}