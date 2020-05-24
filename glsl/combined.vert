#version 430

uniform vec3 Scroll;
uniform vec3 r1, r2;

layout (location = 0 ) in vec3 aVertexPosition;
layout (location = 1 ) in vec2 aTexCoord;
layout (location = 2 ) in vec2 aTexCoordSecond;

layout (location = 3 ) in float aVertexPaletteIndex;
layout (location = 4 ) in float aVertexTexMetadataOption;
layout (location = 5 ) in float aVertexDrawmode;
layout (location = 6 ) in float aVertexTexMetadataOption2;

layout (location = 7 ) in vec4 aVertexColorInfo;
layout (location = 8 ) in vec4 aVertexUVFillRect;

layout (location = 9 ) in float aFlipX;
layout (location = 10 ) in float aFlipY;


out vec2 vTexCoord;
out vec2 vTexCoordSecond;

out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorInfo;
out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
out vec2 fragXY;
out vec2 StartUV;
out vec2 EndUV;
out float VertexTexMetadataOption2;
out float TextureArrayIndex;
out float DrawMode;
out float PaletteIndex;

/* 


vec4 SelectColorFraction(float x)
{
	if (x > 0.0)
		return vec4(0, 0, 0, 0);

	return vec4(1, 1, 1, 1);
}

vec4 SelectRGBAFraction(float x)
{
	if (x == 2.0)
		return vec4(1, 1, 1, 1);

	return vec4(0, 0, 0, 0);
}


 */
 
 
vec4 SelectChannelMask(float x)
{
	if (x == 4.0)
		return vec4(1,1,1,1);
	if (x == 3.0)
		return vec4(0,0,0,1);
	if (x == 2.0)
		return vec4(0,0,1,0);
	if (x == 1.0)
		return vec4(0,1,0,0);

	if (x == 0.0)
		return vec4(1,0,0,0);

	return vec4(0, 0, 0, 0);
}
vec4 SelectPalettedFraction(float x)
{
	//для чего это используеться? в каком алгоритме- не выяснил.
	return vec4(1, 1, 1, 1);
	if (x == 0.0 || x == 2.0)
		return vec4(0, 0, 0, 0);

	
}
void main()
{
 // if aVertexTexMetadata.t=X=65 , primarySampler=1,x=1,primaryChannel=1 =>attrib.s=1=primaryChannel
	vec3 av=aVertexPosition.xyz;
	
	if (aFlipY==1.0)
	{
		//av.xyz=vec3(vec2(av.x, 1.0 - av.y),av.z);
		//av.xyz=vec3(av.xy * vec2(1.0,-1.0),av.z);
	}
	gl_Position = vec4((av.xyz - Scroll.xyz) * r1 + r2, 1);

	VertexTexMetadataOption2=aVertexTexMetadataOption2;
	vTexCoordSecond=aTexCoordSecond;
	vTexCoord=aTexCoord;
	DrawMode=aVertexDrawmode;
	PaletteIndex=aVertexPaletteIndex;
	//vec4 attrib = UnpackChannelAttributes(aVertexTexMetadata.t);
	//drawMode передается в aVertexTexCoord - 4 позиция тип float.
	
	//vec4 drawMode = vec4(aVertexTexMetadata.t;
	
	vColorInfo = aVertexColorInfo; //теперь передаем сразу эти 4 числа float
	
	//vChannelMask = SelectChannelMask(attrib.s);
	//vColorFraction = SelectColorFraction(attrib.s);
	//vRGBAFraction = SelectRGBAFraction(attrib.s);
	//vPalettedFraction = SelectPalettedFraction(attrib.s); primaryChannel=маска канала первая
	//vDepthMask = SelectChannelMask(attrib.t);
	
	//vChannelMask = vec4(aVertexColorInfo.s,0,0,0); 
	vPalettedFraction = SelectPalettedFraction(aVertexColorInfo.s);
	vChannelMask = SelectChannelMask(aVertexColorInfo.s); //динамическое определение маски RGBA
	//тут нужно из целого числа, сделать вектор, чтобы потом умножить и оставить токо ту часть, которая содержит Х коориданату в палитре
	TextureArrayIndex=aVertexColorInfo.s;
	vTexSampler = vec2(0,aVertexColorInfo.t); //номер текстуры

/* 	StartUV=vec2(0.43848,0.10645);
	EndUV=vec2(1.44434,1.11182); 
	EndUV=vec2(0.43848,0.10645);
	StartUV=vec2(1.44434,1.11182);  */
  	StartUV = aVertexUVFillRect.rg;
	EndUV = aVertexUVFillRect.ba; 



} 
