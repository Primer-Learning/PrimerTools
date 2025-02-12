shader_type spatial;
//render_mode world_vertex_coords

varying vec3 model_position;

void vertex() {
	// Called for every vertex the material is visible on.
	model_position = (MODEL_MATRIX * vec4(VERTEX, .0)).xyz;
	//model_position = VERTEX;
}

void fragment() {
	// Called for every pixel the material is visible on.
	ALBEDO = model_position;
	//ALBEDO = VERTEX;
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
