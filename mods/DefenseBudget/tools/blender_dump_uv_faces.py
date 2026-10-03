# Blender side: classify faces by orientation vs the heart's thin (depth) axis and dump UV polygons.
import bpy, bmesh, sys, json
args = sys.argv[sys.argv.index("--") + 1:]
OBJ, OUT = args
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=OBJ)
obj = [o for o in bpy.data.objects if o.type == 'MESH'][0]
d = obj.dimensions; axis_i = min(range(3), key=lambda i: d[i])
bm = bmesh.new(); bm.from_mesh(obj.data); uv = bm.loops.layers.uv.active
faces = []
for f in bm.faces:
    faces.append({"d": abs(f.normal[axis_i]), "uv": [[l[uv].uv.x, l[uv].uv.y] for l in f.loops]})
json.dump({"axis": "xyz"[axis_i], "faces": faces}, open(OUT, "w"))
print("dumped", len(faces), "faces, depth axis", "xyz"[axis_i])
