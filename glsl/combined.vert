#version 430

uniform vec3 Scroll;
uniform vec3 r1, r2;

layout (location = 0 ) in vec3 aVertexPosition;
layout (location = 1 ) in vec4 aVertexTexCoord;
layout (location = 2 ) in vec4 aVertexTexMetadata;
layout (location = 3 ) in vec4 aVertexColorInfo;

out vec4 vTexCoord;
out vec4 vTexMetadata;
out vec4 vChannelMask;
out vec4 vDepthMask;
out vec2 vTexSampler;

out vec4 vColorInfo;
out vec4 vColorFraction;
out vec4 vRGBAFraction;
out vec4 vPalettedFraction;
/* 
vec4 UnpackChannelAttributes(float x)
{
	// The channel attributes float encodes a set of attributes
	// stored as flags in the mantissa of the unnormalized float value.
	// Bits 9-11 define the sampler index (0-7) that the secondary texture is bound to
	// Bits 6-8 define the sampler index (0-7) that the primary texture is bound to
	// Bits 3-5 define the behaviour of the secondary texture channel:
	//    000: Channel is not used
	//    001, 011, 101, 111: Sample depth sprite from channel R,G,B,A
	// Bits 0-2 define the behaviour of the primary texture channel:
	//    000: Channel is not used (aVertexTexCoord instead defines a color value)
	//    010: Sample RGBA sprite from all four channels
	//    001, 011, 101, 111: Sample paletted sprite from channel R,G,B,A
 // if X=65 , primarySampler=1,x=1,primaryChannel=1
 //Unfortunately, OpenGL picked S, T, and R long before GLSL and swizzle masks came around. R, of course, conflicts with R, G, B, and A. To avoid such conflicts, in GLSL, the texture coordinate swizzle mask uses S, T, P, and Q.

//In GLSL, you can swizzle with XYZW, STPQ, or RGBA. They all mean exactly the same thing. So position.st is exactly the same as position.xy. However, you're not allowed to combine swizzle masks from different sets. So position.xt is not allowed.

	float secondarySampler = 0.0;
	if (x >= 2048.0) { x -= 2048.0;  secondarySampler += 4.0; }
	if (x >= 1024.0) { x -= 1024.0;  secondarySampler += 2.0; }
	if (x >= 512.0) { x -= 512.0;  secondarySampler += 1.0; }

	float primarySampler = 0.0;
	if (x >= 256.0) { x -= 256.0;  primarySampler += 4.0; }
	if (x >= 128.0) { x -= 128.0;  primarySampler += 2.0; }
	if (x >= 64.0) { x -= 64.0;  primarySampler += 1.0; }

	float secondaryChannel = 0.0;
	if (x >= 32.0) { x -= 32.0;  secondaryChannel += 4.0; }
	if (x >= 16.0) { x -= 16.0;  secondaryChannel += 2.0; }
	if (x >= 8.0) { x -= 8.0;  secondaryChannel += 1.0; }
	
	float primaryChannel = 0.0;
	if (x >= 4.0) { x -= 4.0;  primaryChannel += 4.0; }
	if (x >= 2.0) { x -= 2.0;  primaryChannel += 2.0; }
	if (x >= 1.0) { x -= 1.0;  primaryChannel += 1.0; }

	return vec4(primaryChannel, secondaryChannel, primarySampler, secondarySampler);
}


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
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
	vTexCoord = aVertexTexCoord;
	vTexMetadata = aVertexTexMetadata;
	
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
	
	vTexSampler = vec2(0,aVertexColorInfo.t); //номер текстуры
} 
