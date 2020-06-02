#version 430

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
uniform vec3 TextColor;

in vec2 vTexCoord;
in vec4 vTexMetadata;
in vec4 vChannelMask;
in vec4 vDepthMask;
in vec2 vTexSampler;

in vec4 vColorInfo;
in vec4 vColorFraction;
in vec4 vRGBAFraction;
in vec4 vPalettedFraction;
in vec4 vTextColor;


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

/* 	vec3 flipped_texCoords = vec3(vTexCoord.s, 1.0 - vTexCoord.t, vTexMetadata.s);
    vec2 pos = flipped_texCoords.xy;
    vec3 sample1 = texture(TextureFontMSDF, flipped_texCoords).rgb;
    ivec2 sz = textureSize(TextureFontMSDF, 0).xy;
    float dx = dFdx(pos.x) * sz.x; 
    float dy = dFdy(pos.y) * sz.y;
    float toPixels = 12 * inversesqrt(dx * dx + dy * dy);
    float sigDist = median(sample1.r, sample1.g, sample1.b);
    float w = fwidth(sigDist);
    float opacity = smoothstep(0.5 - w, 0.5 + w, sigDist);
    fragColor = vec4(TextColor.rgb, opacity);
	 */
	
	
	vec3 flipped_texCoords = vec3(vTexCoord.s,1.0-vTexCoord.t,vTexMetadata.s); 	//vec3 flipped_texCoords = vec3(0.0,0.0,84);
	
	vec3 samplemsdf = texture(TextureFontMSDF, flipped_texCoords).rgb;
	
	vec2 pos = flipped_texCoords.xy;
    
	float pxRange=12; //так было при генерации png в msdfgen.

	vec2 onepixel=1.0/vec2(textureSize(TextureFontMSDF, 0));

	vec2 msdfUnit = pxRange/vec2(textureSize(TextureFontMSDF, 0));
    float sigDist = median(samplemsdf.r, samplemsdf.g, samplemsdf.b) - 0.5;
    sigDist *= dot(msdfUnit, 0.5/fwidth(pos));
    float opacity = clamp(sigDist + 0.5, 0.0, 1.0);

	vec4 tempcolor;
	fragColor =vec4(TextColor.rgb,opacity ) ; 
/* lumination
	vec4 tl = texture(TextureFontMSDF, flipped_texCoords - vec3(-onepixel,  0));
   vec4 br = texture(TextureFontMSDF, flipped_texCoords + vec3( onepixel,  0));
   vec4 color0 = vec4(opacity);
   vec4 sum=(2.0*tl-color0-br);
   float luminance = clamp(0.299 * sum.r + 0.587 * sum.g + 0.114 * sum.b,0.0,1.0);
   sum = vec4( 0.5, 0.5, 0.5, 1.0 ) + vec4( luminance,luminance,luminance,1.0 );
	float fade_const=0.4;
   //fragColor = vec4(((opacity - fade_const) * color0 + fade_const * sum).rgb ,opacity );	
   fragColor =((opacity - fade_const) * color0 + fade_const * sum * opacity);	 */
  
  //emboss or bevel
/* 	vec3 color ;
	color = vec3(opacity);
	
	color += texture(TextureFontMSDF,flipped_texCoords - vec3(onepixel,0)).rgb * 3;
	color -= texture(TextureFontMSDF,flipped_texCoords + vec3(onepixel,0)).rgb * 3;
	color = vec3((color.r + color.g + color.b )/3.0);
	fragColor =vec4(color,opacity) ;  */
	 
	//fragColor = vec4(fragColor.rgb ,opacity); 
	//if(fragColor.a <0.1)
     // discard;
	//fragColor =fragColor;
    //vec3 NewColor = vec3(1,1,1);//можно с CPU передавать разные цвета
  //fragColor =vec4(vColorInfo.rgb,opacity);

	
	
	
	
}
