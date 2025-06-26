extends Node2D

# Configuration
@export var curve_height: float = 300.0
@export var curve_width: float = 800.0
@export var duration: float = 3.0
@export var dot_size: float = 4.0

# Current settings
var current_trans: Tween.TransitionType = Tween.TRANS_LINEAR
var current_ease: Tween.EaseType = Tween.EASE_IN_OUT

# Visual elements
var tracer_dot: Node2D
var trail_points: PackedVector2Array
var start_pos: Vector2
var tween: Tween

func _ready():
	# Create the tracer dot
	tracer_dot = Node2D.new()
	add_child(tracer_dot)
	tracer_dot.position = Vector2(50, 400)
	start_pos = tracer_dot.position

	# Set up UI
	create_ui()

	# Start the animation
	start_animation()

func create_ui():
	# Create labels and buttons for transition types
	var trans_label = Label.new()
	trans_label.text = "Transition Type:"
	trans_label.position = Vector2(10, 10)
	add_child(trans_label)

	var trans_names = ["LINEAR", "SINE", "QUINT", "QUART", "QUAD", "EXPO",
					  "ELASTIC", "CUBIC", "CIRC", "BOUNCE", "BACK", "SPRING"]

	for i in range(trans_names.size()):
		var btn = Button.new()
		btn.text = trans_names[i]
		btn.position = Vector2(10 + (i % 6) * 130, 40 + (i / 6) * 30)
		btn.size = Vector2(120, 25)
		btn.pressed.connect(_on_trans_selected.bind(i))
		add_child(btn)

	# Create labels and buttons for ease types
	var ease_label = Label.new()
	ease_label.text = "Ease Type:"
	ease_label.position = Vector2(10, 110)
	add_child(ease_label)

	var ease_names = ["IN", "OUT", "IN_OUT", "OUT_IN"]

	for i in range(ease_names.size()):
		var btn = Button.new()
		btn.text = ease_names[i]
		btn.position = Vector2(10 + i * 130, 140)
		btn.size = Vector2(120, 25)
		btn.pressed.connect(_on_ease_selected.bind(i))
		add_child(btn)

	# Current settings label
	var current_label = Label.new()
	current_label.name = "CurrentLabel"
	current_label.position = Vector2(10, 180)
	add_child(current_label)
	update_current_label()

func _on_trans_selected(trans_type: int):
	current_trans = trans_type as Tween.TransitionType
	restart_animation()

func _on_ease_selected(ease_type: int):
	current_ease = ease_type as Tween.EaseType
	restart_animation()

func update_current_label():
	var label = get_node("CurrentLabel")
	if label:
		label.text = "Current: TRANS_" + get_trans_name(current_trans) + " + EASE_" + get_ease_name(current_ease)

func get_trans_name(trans: Tween.TransitionType) -> String:
	var names = ["LINEAR", "SINE", "QUINT", "QUART", "QUAD", "EXPO",
				"ELASTIC", "CUBIC", "CIRC", "BOUNCE", "BACK", "SPRING"]
	return names[trans]

func get_ease_name(ease: Tween.EaseType) -> String:
	var names = ["IN", "OUT", "IN_OUT", "OUT_IN"]
	return names[ease]

func restart_animation():
	if tween:
		tween.kill()
	trail_points.clear()
	queue_redraw()
	update_current_label()
	start_animation()

func start_animation():
	# Reset position
	tracer_dot.position = start_pos

	# Create the animation
	tween = create_tween()
	tween.set_loops()

	# Horizontal movement (constant speed - no tween)
	# This represents time passing linearly
	tween.parallel().tween_property(tracer_dot, "position:x", start_pos.x + curve_width, duration)

	# Vertical movement (tweened)
	# This represents the actual tween progress
	tween.parallel().tween_property(tracer_dot, "position:y", start_pos.y - curve_height, duration) \
	.set_trans(current_trans) \
	.set_ease(current_ease)

	# Reset for loop
	tween.tween_callback(reset_position)

func reset_position():
	tracer_dot.position = start_pos
	trail_points.clear()

func _process(_delta):
	# Add current position to trail
	if tracer_dot:
		trail_points.append(tracer_dot.position)
		# Limit trail length to avoid memory issues
		if trail_points.size() > 1000:
			trail_points.remove_at(0)
		queue_redraw()

func _draw():
	# Draw axes
	draw_line(start_pos, start_pos + Vector2(curve_width, 0), Color.GRAY, 2.0)
	draw_line(start_pos, start_pos + Vector2(0, -curve_height), Color.GRAY, 2.0)

	# Draw grid lines
	for i in range(5):
		var x = start_pos.x + (i + 1) * curve_width / 5
		draw_line(Vector2(x, start_pos.y), Vector2(x, start_pos.y - curve_height),
		Color(0.3, 0.3, 0.3, 0.3), 1.0)

		var y = start_pos.y - (i + 1) * curve_height / 5
		draw_line(Vector2(start_pos.x, y), Vector2(start_pos.x + curve_width, y),
		Color(0.3, 0.3, 0.3, 0.3), 1.0)

	# Draw the trail
	if trail_points.size() > 1:
		for i in range(1, trail_points.size()):
			var alpha = float(i) / float(trail_points.size())
			draw_circle(trail_points[i], dot_size, Color(1.0, 0.3, 0.3, alpha))

	# Draw current position
	if tracer_dot:
		draw_circle(tracer_dot.position, dot_size * 1.5, Color.RED)
		