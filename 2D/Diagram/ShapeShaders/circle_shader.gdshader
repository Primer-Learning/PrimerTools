shader_type spatial;
render_mode cull_disabled;

uniform vec4 shape_color : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 background_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);
uniform float thickness : hint_range(0.01, 0.5) = 0.1;
uniform float smoothness : hint_range(0.001, 0.1) = 0.01;
uniform int shape_type : hint_range(0, 4) = 0;

// Shape-specific parameters
uniform vec2 shape_center = vec2(0.0, 0.0);  // Center in object space
uniform float circle_radius = 0.7;
uniform vec2 box_size = vec2(0.6, 0.6);

varying vec2 object_pos;

void vertex() {
    // Pass object-space position to fragment shader
    object_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xy;
}

float sdCircle(vec2 p, float r) {
    return length(p) - r;
}

float makeHollow(float d, float thick) {
    return abs(d) - thick;
}

void fragment() {
    // Use object-space coordinates instead of UV
    vec2 p = object_pos - shape_center;

    float d = sdCircle(p, circle_radius);

    // Make the shape hollow
    d = makeHollow(d, thickness);

    // Apply smoothstep for antialiasing
    float alpha = 1.0 - smoothstep(0.0, smoothness, d);

    // Mix colors based on the SDF
    vec3 color = mix(background_color.rgb, shape_color.rgb, alpha);
    float final_alpha = mix(background_color.a, shape_color.a, alpha);

    ALBEDO = color;
    ALPHA = final_alpha;
}