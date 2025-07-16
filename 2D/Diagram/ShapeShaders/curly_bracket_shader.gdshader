shader_type spatial;
render_mode cull_disabled;

#include "sdf_primitives.gdshaderinc"
#include "sdf_common.gdshaderinc"

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0, 0.5) = 0.02;
uniform float thickness_variation : hint_range(0.0, 5.0) = 2.0;
uniform float smoothness : hint_range(0, 10) = 0.01;

// Bracket parameters
uniform vec2 bracket_tip1 = vec2(-0.5, -1.0);
uniform vec2 bracket_tip2 = vec2(0.5, -1.0);
uniform vec2 bracket_stem = vec2(0.0, 0.0);
uniform vec2 mesh_size = vec2(2.0, 2.0);

varying vec2 object_pos;

void vertex() {
    object_pos = vec2(VERTEX.x, -VERTEX.z);
}

//void fragment() {
	////if (object_pos.x > 1.0 && object_pos.y > 1.0) ALBEDO = vec3(0.0);
	////else ALBEDO = vec3(object_pos, 1.0);
    //vec2 p = object_pos;
    //
    //// Evaluate curly bracket with variable thickness
    //float d = sdCurlyBracketVariable(p, bracket_tip1, bracket_tip2, bracket_stem, thickness, thickness_variation);
    //
    //// Apply smoothstep for antialiasing
    //float alpha = 1.0 - smoothstep(0.0, smoothness, d);
    //
    //// Mix colors based on the SDF
    //vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    //float final_alpha = mix(background_color.a, shape_color.a, alpha);
    //
    //ALBEDO = color;
    //ALPHA = final_alpha;
//}

void fragment() {
    vec2 p = object_pos;

    // Evaluate curly bracket with variable thickness
    float d = sdCurlyBracketVariable(
		p, bracket_tip1, bracket_tip2, bracket_stem, thickness, thickness_variation);

    // Calculate screen-space derivatives of the distance field itself
    float d_dx = dFdx(d);
    float d_dy = dFdy(d);
    float grad_len = length(vec2(d_dx, d_dy));

    // Normalize the gradient to get consistent AA width
    float aa_width = smoothness * max(grad_len, 0.0001);

    // Apply smoothstep with gradient-based anti-aliasing
    float alpha = 1.0 - smoothstep(0.0, aa_width, d);

    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);

    ALBEDO = color;
    ALPHA = final_alpha;
}
