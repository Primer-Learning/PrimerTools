shader_type spatial;
render_mode cull_disabled;

#include "sdf_primitives.gdshaderinc"
#include "sdf_common.gdshaderinc"

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0, 2) = 0.1;
uniform float smoothness : hint_range(0, 2) = 0.01;

// Arrow-specific parameters
uniform vec2 arrow_start = vec2(-1.0, 0.0);
uniform vec2 arrow_end = vec2(1.0, 0.0);
uniform float head_length = 0.3;
uniform float head_angle = 30.0; // degrees

varying vec2 object_pos;

void vertex() {
    // Pass object-space position to fragment shader
	object_pos = vec2(VERTEX.x, -VERTEX.z);
}

//void fragment() {
    //// Use object-space coordinates
    //vec2 p = object_pos;
//
    //float d = sdArrow(p, arrow_start, arrow_end, head_length, head_angle);
    //d = d - thickness;
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
    float d = sdArrow(p, arrow_start, arrow_end, head_length, head_angle);
    d = d - thickness;

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