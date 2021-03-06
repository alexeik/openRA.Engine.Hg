#version 430

uniform vec3 Scroll;
uniform vec3 r1, r2;

layout (location = 0 ) in vec3 aVertexPosition;
layout (location = 1 ) in vec2 aTexCoord;
layout (location = 2 ) in vec2 aTexCoordSecond;
layout (location = 3 ) in vec4 aVertexTexMetadata;
layout (location = 4 ) in vec4 aVertexColorInfo;



out vec2 vTexCoord;
out vec4 vTexMetadata;
out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorInfo;
out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
out vec4 vTextColor;

void main()
{
 // if aVertexTexMetadata.t=X=65 , primarySampler=1,x=1,primaryChannel=1 =>attrib.s=1=primaryChannel
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aTexCoord;
	vTexMetadata = aVertexTexMetadata;
	
	//vec4 attrib = UnpackChannelAttributes(aVertexTexMetadata.t);
	//drawMode передается в aVertexTexMetadata - 2 позиция тип float.
	
	//vec4 drawMode = vec4(aVertexTexMetadata.t;
	
	vColorInfo = aVertexColorInfo; //теперь передаем сразу эти 4 числа float

	//vChannelMask = SelectChannelMask(attrib.s);
	//vColorFraction = SelectColorFraction(attrib.s);
	//vRGBAFraction = SelectRGBAFraction(attrib.s);
	//vPalettedFraction = SelectPalettedFraction(attrib.s);
	//vDepthMask = SelectChannelMask(attrib.t);
	
	vChannelMask = vec4(aVertexColorInfo.s,0,0,0); 
	//тут нужно из целого числа, сделать вектор, чтобы потом умножить и оставить токо ту часть, которая содержит Х коориданату в палитре
	//vTextColor= vec4(TextColor,1);
	vTexSampler = vec2(aVertexColorInfo.t,0);
} 
