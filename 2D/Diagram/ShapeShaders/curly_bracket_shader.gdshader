shader_type spatial;
render_mode cull_disabled;

#include "sdf_primitives.gdshaderinc"
#include "sdf_common.gdshaderinc"

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float base_thickness : hint_range(0.01, 0.5) = 0.02;
uniform float thickness_variation : hint_range(0.0, 5.0) = 2.0;
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;

// Bracket parameters
uniform vec2 bracket_tip1 = vec2(-0.5, -1.0);
uniform vec2 bracket_tip2 = vec2(0.5, -1.0);
uniform vec2 bracket_stem = vec2(0.0, 0.0);
uniform vec2 mesh_size = vec2(2.0, 2.0);

varying vec2 object_pos;

void vertex() {
    // VERTEX is in normalized coordinates for a plane mesh
    // Scale by mesh size to get actual object coordinates
    object_pos = vec2(VERTEX.x, -VERTEX.z);
	
	// old object_pos, which is world-space xy position of the vertex
    //object_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xy;
	// new object_pos is the local xy position of the vertex
	//object_pos = ((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz - NODE_POSITION_WORLD).xy;
	// The below makes a very warped result.
	// MODEL_MATRIX seems to do some scaling. I'm not sure of the units of VERTEX.
	// Is it just UV space?
	// object_pos = VERTEX.xy
	// We need to rotate 
}

void fragment() {
	
	//if (object_pos.x > 1.0 && object_pos.y > 1.0) ALBEDO = vec3(0.0);
	//else ALBEDO = vec3(object_pos, 1.0);
    vec2 p = object_pos;
    
    // Evaluate curly bracket with variable thickness
    float d = sdCurlyBracketVariable(p, bracket_tip1, bracket_tip2, bracket_stem, base_thickness, thickness_variation);
    
    // Apply smoothstep for antialiasing
    float alpha = 1.0 - smoothstep(0.0, smoothness, d);
    
    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);
    
    ALBEDO = color;
    ALPHA = final_alpha;
}
