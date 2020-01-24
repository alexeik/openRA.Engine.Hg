#version 130

out vec4 fragColor;

uniform sampler2D Texture0;
uniform sampler2D Texture1;
uniform sampler2D Texture2;
uniform sampler2D Texture3;
uniform sampler2D Texture4;
uniform sampler2D Texture5;
uniform sampler2D Texture6;
uniform sampler2D Palette;
uniform sampler2DArray TextureFontMSDF;

uniform bool EnableDepthPreview;
uniform float DepthTextureScale;


in vec4 vTexCoord;
in vec2 vTexMetadata;
in vec4 vChannelMask;
in vec4 vDepthMask;
in vec2 vTexSampler;

in vec4 vColorInfo;
in vec4 vColorFraction;
in vec4 vRGBAFraction;
in vec4 vPalettedFraction;
in vec4 vTextColor;


vec4 Sample(float samplerIndex, vec2 pos)
{
	if (samplerIndex < 0.5)
		return texture2D(Texture0, pos);
	else if (samplerIndex < 1.5)
		return texture2D(Texture1, pos);
	else if (samplerIndex < 2.5)
		return texture2D(Texture2, pos);
	else if (samplerIndex < 3.5)
		return texture2D(Texture3, pos);
	else if (samplerIndex < 4.5)
		return texture2D(Texture4, pos);
	else if (samplerIndex < 5.5)
		return texture2D(Texture5, pos);

	return texture2D(Texture6, pos);
}
float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

void main()
{
	/* vec4 x = Sample(vTexSampler.s, vTexCoord.st);
	vec2 p = vec2(dot(x, vChannelMask), vTexMetadata.s);
	vec4 c = vPalettedFraction * texture2D(Palette, p) + vRGBAFraction * x + vColorFraction * vTexCoord;
 */

	//fragColor= texture(TextureFontMSDF, vec3(0,0,1)); 

		
	vec3 flipped_texCoords = vec3(vTexCoord.s,vTexCoord.t,vColorInfo.p); 	//vec3 flipped_texCoords = vec3(0.0,0.0,84);
	
	vec3 sample = texture(TextureFontMSDF, flipped_texCoords).rgb;
	
	vec2 pos = flipped_texCoords.xy;
    
	float pxRange=12; //так было при генерации png в msdfgen.
	
	vec2 msdfUnit = pxRange/vec2(textureSize(TextureFontMSDF, 0));
    float sigDist = median(sample.r, sample.g, sample.b) - 0.5;
    sigDist *= dot(msdfUnit, 0.5/fwidth(pos));
    float opacity = clamp(sigDist + 0.5, 0.0, 1.0);
	
	fragColor = vec4(vTextColor.rgb , opacity); 
    //fragColor = vec4(vec3(0,0,0) , opacity); 
	/*
    ivec2 sz =  textureSize(TextureFontMSDF, 0).xy; // ivec2(96,96); // возвращает h & w текстуры буквы
    float dx = dFdx(pos.x) * sz.x; 
    float dy = dFdy(pos.y) * sz.y;
    float toPixels = 8.0 * inversesqrt(dx * dx + dy * dy);
    float sigDist = median(sample.r, sample.g, sample.b);
    float w = fwidth(sigDist);
    float opacity = smoothstep(0.5 - w, 0.5 + w, sigDist); 

    fragColor = vec4(vTextColor, opacity);*/
	//c=vec4(sample,1);
	
	// orig vec4 x = Sample(vTexSampler.s, vTexCoord.st); //возвращает структуру (R,G,B,A) из текстуры
	//vec4 c = vRGBAFraction * x ;
	// orig c =  x ; // vRGBAFraction всегда 1,1,1,1
	
	
	// Discard any transparent fragments (both color and depth)
	// if (c.a == 0.0)
	//	discard; 

	
		/* if ( c.a != -1) 
		{
						// Get the neighbouring four pixels.
			vec2 textureSize2d =textureSize(Texture1,0);
			float textureSize = float(textureSize2d.x);
			float texelSize = 0.0019;

			
			vec4 pixelUp = Sample(vTexSampler.s, vTexCoord.st+ vec2(0, texelSize));
			vec4 pixelDown = Sample(vTexSampler.s, vTexCoord.st - vec2(0, texelSize));
			vec4 pixelRight = Sample(vTexSampler.s, vTexCoord.st + vec2(texelSize, 0));
			vec4 pixelLeft = Sample(vTexSampler.s, vTexCoord.st - vec2(texelSize, 0));

						// If one of the neighbouring pixels is invisible, we render an outline.
			//if (pixelUp.a * pixelDown.a * pixelRight.a * pixelLeft.a == 0) 
			//{
				//c.rgba =  vec4(1,0,0,0.5); убрал, потому что на маленьких размерах, outline смотрится плохо.
			//}
			//----
			//vec2 onePixel=vec2(0.0019,0.0019);
			//vec2 texCoord= vTexCoord.st;
			  // 4
			  //vec4 color;
			  //color.rgb = vec3(0.5);
			  // vec3 colororig=Sample(vTexSampler.s, vTexCoord.st).rgb;
			  //color -=  Sample(vTexSampler.s, texCoord - onePixel) * 5.0;
			  //color += Sample(vTexSampler.s, texCoord + onePixel) * 5.0;
			  // 5
			  //color.rgb = vec3((color.r + color.g + color.b) / 3.0);
			  //fragColor = vec4(color.rgb*colororig, 1);
		} */

       //c.rgb *= c.a;
				
		//fragColor = c;
		

	
	
	
}
