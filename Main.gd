extends Node2D

const GRID_SIZE := 32
const BOARD_WIDTH := 24
const BOARD_HEIGHT := 16
const INITIAL_SNAKE_LEN := 4
const MOVE_INTERVAL := 0.18

var snake: Array[Vector2i] = []
var direction := Vector2i.RIGHT
var pending_direction := Vector2i.RIGHT
var grow_pending := 0
var fruit := Vector2i(15, 10)
var obstacles: Dictionary = {}
var game_over := false
var move_accumulator := 0.0
var rng := RandomNumberGenerator.new()

func _ready() -> void:
	rng.randomize()
	_create_level_obstacles()
	_reset_game()

func _process(delta: float) -> void:
	if game_over:
		if Input.is_action_just_pressed("ui_accept"):
			_reset_game()
		queue_redraw()
		return

	_read_input()
	move_accumulator += delta
	while move_accumulator >= MOVE_INTERVAL:
		move_accumulator -= MOVE_INTERVAL
		_step_game()
		if game_over:
			break

	queue_redraw()

func _read_input() -> void:
	if Input.is_action_just_pressed("ui_left"):
		pending_direction = Vector2i.LEFT
	elif Input.is_action_just_pressed("ui_right"):
		pending_direction = Vector2i.RIGHT
	elif Input.is_action_just_pressed("ui_up"):
		pending_direction = Vector2i.UP
	elif Input.is_action_just_pressed("ui_down"):
		pending_direction = Vector2i.DOWN

func _step_game() -> void:
	if pending_direction + direction != Vector2i.ZERO:
		direction = pending_direction

	var next_head := snake[0] + direction
	if _is_outside(next_head) or _is_obstacle(next_head) or _snake_contains(next_head):
		game_over = true
		return

	snake.push_front(next_head)
	if grow_pending > 0:
		grow_pending -= 1
	else:
		snake.pop_back()

	_try_eat_fruit()
	_apply_gravity()

func _apply_gravity() -> void:
	while not _is_supported():
		var fallen: Array[Vector2i] = []
		for segment in snake:
			var down := segment + Vector2i.DOWN
			if _is_outside(down):
				game_over = true
				return
			fallen.append(down)
		snake = fallen
		_try_eat_fruit()

func _is_supported() -> bool:
	for segment in snake:
		var down := segment + Vector2i.DOWN
		if _is_outside(down) or _is_obstacle(down):
			return true
		if _snake_contains(down):
			return true
	return false

func _try_eat_fruit() -> void:
	if snake[0] == fruit:
		grow_pending += 1
		_spawn_fruit()

func _spawn_fruit() -> void:
	var free_cells: Array[Vector2i] = []
	for y in range(BOARD_HEIGHT):
		for x in range(BOARD_WIDTH):
			var cell := Vector2i(x, y)
			if not _is_obstacle(cell) and not _snake_contains(cell):
				free_cells.append(cell)

	if free_cells.is_empty():
		return

	fruit = free_cells[rng.randi_range(0, free_cells.size() - 1)]

func _snake_contains(cell: Vector2i) -> bool:
	for segment in snake:
		if segment == cell:
			return true
	return false

func _is_obstacle(cell: Vector2i) -> bool:
	return obstacles.has(cell)

func _is_outside(cell: Vector2i) -> bool:
	return cell.x < 0 or cell.x >= BOARD_WIDTH or cell.y < 0 or cell.y >= BOARD_HEIGHT

func _reset_game() -> void:
	snake.clear()
	for i in range(INITIAL_SNAKE_LEN):
		snake.append(Vector2i(6 - i, 4))
	direction = Vector2i.RIGHT
	pending_direction = direction
	grow_pending = 0
	game_over = false
	move_accumulator = 0.0
	_spawn_fruit()

func _create_level_obstacles() -> void:
	obstacles.clear()
	# 中央平台
	for x in range(4, 12):
		obstacles[Vector2i(x, 9)] = true
	# 右侧高台
	for y in range(6, 11):
		obstacles[Vector2i(17, y)] = true
	for x in range(15, 20):
		obstacles[Vector2i(x, 6)] = true
	# 左下方台阶
	for x in range(1, 5):
		obstacles[Vector2i(x, 13)] = true
	obstacles[Vector2i(4, 12)] = true

func _draw() -> void:
	var board_rect := Rect2(Vector2.ZERO, Vector2(BOARD_WIDTH, BOARD_HEIGHT) * GRID_SIZE)
	draw_rect(board_rect, Color("1f1f30"), true)
	draw_rect(board_rect, Color("6fa6ff"), false, 3.0)

	for cell in obstacles.keys():
		_draw_cell(cell, Color("4b5a76"))

	for i in range(snake.size() - 1, -1, -1):
		var color := Color("50e36c") if i == 0 else Color("1bcf64")
		_draw_cell(snake[i], color)

	_draw_cell(fruit, Color("ff5d5d"))

	if game_over:
		draw_string(get_theme_default_font(), Vector2(140, 220), "Game Over! Enter 重开", HORIZONTAL_ALIGNMENT_LEFT, -1, 28, Color.WHITE)

	draw_string(get_theme_default_font(), Vector2(10, 24), "方向键移动 | 重力持续生效", HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color("cce4ff"))

func _draw_cell(cell: Vector2i, color: Color) -> void:
	var pos := Vector2(cell.x, cell.y) * GRID_SIZE
	draw_rect(Rect2(pos + Vector2.ONE * 1.0, Vector2.ONE * (GRID_SIZE - 2)), color, true)
