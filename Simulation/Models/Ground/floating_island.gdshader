shader_type spatial;

uniform vec4 top_color : source_color = vec4(0.133, 0.545, 0.133, 1.0);  // Green
uniform vec4 bottom_color : source_color = vec4(0.545, 0.271, 0.075, 1.0);  // Brown
uniform float transition_height : hint_range(-1.0, 1.0) = 0.0;
uniform float transition_smoothness : hint_range(0.0, 1.0) = 0.5;
uniform float noise_scale : hint_range(0.0, 5.0) = 1.0;
uniform float noise_influence : hint_range(0.0, 0.5) = 0.1;
uniform float shadow_strength : hint_range(0.0, 1.0) = 0.5;
uniform float shadow_smoothness : hint_range(0.01, 1.0) = 0.1;
uniform float specular_intensity : hint_range(0.0, 1.0) = 0.1;
uniform float specular_shininess : hint_range(1.0, 100.0) = 50.0;

varying vec3 model_space_vertex_position;
varying vec3 model_space_normal;

// 2D Simplex noise implementation
vec3 permute(vec3 x) { return mod(((x*34.0)+1.0)*x, 289.0); }
float simplex2D(vec2 v) {
	const vec4 C = vec4(0.211324865405187, 0.366025403784439,
		-0.577350269189626, 0.024390243902439);
	vec2 i  = floor(v + dot(v, C.yy));
	vec2 x0 = v -   i + dot(i, C.xx);
	vec2 i1;
	i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
	vec4 x12 = x0.xyxy + C.xxzz;
	x12.xy -= i1;
	i = mod(i, 289.0);
	vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
	                  + i.x + vec3(0.0, i1.x, 1.0 ));
	vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy),
		dot(x12.zw,x12.zw)), 0.0);
	m = m*m ;
	m = m*m ;
	vec3 x = 2.0 * fract(p * C.www) - 1.0;
	vec3 h = abs(x) - 0.5;
	vec3 ox = floor(x + 0.5);
	vec3 a0 = x - ox;
	m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
	vec3 g;
	g.x  = a0.x  * x0.x  + h.x  * x0.y;
	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
	return 130.0 * dot(m, g);
}

float map_smoothness(float value) {
    return value * 0.04;
}

float region_assignment(vec3 position) {
    float height = position.y;
    vec2 noise_input = position.xz * noise_scale;
    float noise_value = simplex2D(noise_input);
    float noise_offset = noise_value * noise_influence;
    float adjusted_transition_height = transition_height + noise_offset;
    float mapped_smoothness = map_smoothness(transition_smoothness);
    return smoothstep(
        adjusted_transition_height - mapped_smoothness,
        adjusted_transition_height + mapped_smoothness,
        height
    );
}

void vertex() {
    model_space_vertex_position = VERTEX;
    model_space_normal = NORMAL;
}

void fragment() {
    float region = region_assignment(model_space_vertex_position);

    // Calculate the gradient of the region assignment
    vec2 epsilon = vec2(0.01, 0.0);
    float gx = region_assignment(model_space_vertex_position + epsilon.xyy) - region_assignment(model_space_vertex_position - epsilon.xyy);
    float gy = region_assignment(model_space_vertex_position + epsilon.yxy) - region_assignment(model_space_vertex_position - epsilon.yxy);
    float gz = region_assignment(model_space_vertex_position + epsilon.yyx) - region_assignment(model_space_vertex_position - epsilon.yyx);

    // Create a normal perturbation based on the gradient
    vec3 normal_perturbation = -vec3(gx, gy, gz) * shadow_strength;

    // Mix between the original normal and the perturbed normal based on proximity to the transition
    float transition_proximity = 1.0 - abs(region - 0.5) * 2.0;
    transition_proximity = smoothstep(0.0, shadow_smoothness, transition_proximity);

    // Perturb the model space normal
    vec3 perturbed_normal = normalize(model_space_normal + normal_perturbation * transition_proximity);
    // Transform the perturbed normal to view space
    perturbed_normal = (VIEW_MATRIX * MODEL_MATRIX * vec4(perturbed_normal, 0.0)).xyz;
    NORMAL = normalize(perturbed_normal);

    // Mix the colors based on the region
    ALBEDO = mix(bottom_color.rgb, top_color.rgb, region);
}

//void light() {
    //// Calculate the dot product between the light direction and the surface normal
    //float NdotL = max(dot(NORMAL, LIGHT), 0.0);
//
    //// Calculate the region again (we can't pass it from fragment to light)
    //float region = region_assignment(model_space_vertex_position);
//
    //// Calculate how close we are to the transition
    //float transition_proximity = 1.0 - abs(region - 0.5) * 2.0;
    //transition_proximity = smoothstep(0.0, shadow_smoothness, transition_proximity);
//
    //// Calculate the shadow factor based on the region and light direction
    //float shadow_factor = 1.0;
    //float light_alignment = dot(normalize(LIGHT), vec3(0.0, 1.0, 0.0));
    //if (light_alignment > 0.0) {
        //shadow_factor = mix(1.0, 0.5, transition_proximity * (1.0 - region));
    //} else if (light_alignment < 0.0) {
        //shadow_factor = mix(1.0, 0.5, transition_proximity * region);
    //}
//
    //// Calculate the ambient light
    //vec3 ambient_light = ALBEDO * 0.2;
//
    //// Apply the shadow factor and ambient light to the diffuse light
    //DIFFUSE_LIGHT += (ALBEDO * LIGHT_COLOR * NdotL * shadow_factor + ambient_light) * ATTENUATION;
//
    //// Add a subtle specular reflection
    //vec3 half = normalize(VIEW + LIGHT);
    //float NdotH = max(dot(NORMAL, half), 0.0);
    //float specular = pow(NdotH, specular_shininess) * specular_intensity;
    //SPECULAR_LIGHT += specular * LIGHT_COLOR * shadow_factor * ATTENUATION;
//}
