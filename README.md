## Installation

```
git submodule 'git@github.com:Quadtree/godotcslib.git' cslib
```

## Updating

```
git pull --recurse-submodules
```

## Initializing after Clone
```
git submodule update --init
```

## Standard Input Setup

```
[input]

movement_forward={
"deadzone": 0.5,
"events": [ Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":0,"alt":false,"shift":false,"control":false,"meta":false,"command":false,"pressed":false,"scancode":87,"unicode":0,"echo":false,"script":null)
 ]
}
movement_backward={
"deadzone": 0.5,
"events": [ Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":0,"alt":false,"shift":false,"control":false,"meta":false,"command":false,"pressed":false,"scancode":83,"unicode":0,"echo":false,"script":null)
 ]
}
movement_left={
"deadzone": 0.5,
"events": [ Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":0,"alt":false,"shift":false,"control":false,"meta":false,"command":false,"pressed":false,"scancode":65,"unicode":0,"echo":false,"script":null)
 ]
}
movement_right={
"deadzone": 0.5,
"events": [ Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":0,"alt":false,"shift":false,"control":false,"meta":false,"command":false,"pressed":false,"scancode":68,"unicode":0,"echo":false,"script":null)
 ]
}
movement_jump={
"deadzone": 0.5,
"events": [ Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":0,"alt":false,"shift":false,"control":false,"meta":false,"command":false,"pressed":false,"scancode":32,"unicode":0,"echo":false,"script":null)
 ]
}
```