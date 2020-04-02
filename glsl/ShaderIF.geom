#version 430

out vec2 originXY;
layout (triangles) in;
layout (triangle_strip ,max_vertices =4) out;

void main ()
{


	gl_Position = gl_in[0].gl_Position;
  EmitVertex();
    gl_Position = gl_in[1].gl_Position;
  EmitVertex();
    gl_Position = gl_in[2].gl_Position;
  EmitVertex();
    gl_Position = gl_in[3].gl_Position;
  EmitVertex();
  
  EndPrimitive();
  
originXY  = gl_in[0].gl_Position.xy;

}