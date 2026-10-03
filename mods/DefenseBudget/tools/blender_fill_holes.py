# Fill the open boundary loops in the TRELLIS side wall (they read as a dark sawtooth),
# orient each new face like its neighbours, and map new faces onto a solid side-magenta texel.
import bpy, bmesh, sys
import numpy as np
OBJ_IN, TEX, OBJ_OUT = sys.argv[sys.argv.index("--") + 1:]
N = 512
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=OBJ_IN)
obj = [o for o in bpy.data.objects if o.type == 'MESH'][0]
bm = bmesh.new(); bm.from_mesh(obj.data)
uv = bm.loops.layers.uv.active
before = sum(1 for e in bm.edges if e.is_boundary)
old_faces = set(bm.faces)
res = bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary], sides=64)
new_faces = [f for f in bm.faces if f not in old_faces]
tri = bmesh.ops.triangulate(bm, faces=new_faces)
new_faces = [f for f in bm.faces if f not in old_faces]
bm.normal_update()
flipped = 0
for f in new_faces:
    nbr = [lf for e in f.edges for lf in e.link_faces if lf is not f and lf in old_faces]
    if nbr:
        avg = sum((lf.normal for lf in nbr), f.normal * 0)
        if f.normal.dot(avg) < 0:
            bmesh.ops.reverse_faces(bm, faces=[f]); flipped += 1
# a texel that the repaint set to the flat side magenta (bottom-up rows = Blender UV v)
tex = np.frombuffer(open(TEX, 'rb').read(), dtype=np.uint8).reshape(N, N, 4)
target = np.array([233, 30, 117])
idx = np.argwhere(np.all(np.abs(tex[..., :3].astype(int) - target) <= 1, axis=-1))
r, c = idx[len(idx) // 2]
u, v = (c + 0.5) / N, (r + 0.5) / N
for f in new_faces:
    for l in f.loops:
        l[uv].uv = (u, v)
bm.to_mesh(obj.data); bm.free(); obj.data.update()
bm2 = bmesh.new(); bm2.from_mesh(obj.data)
after = sum(1 for e in bm2.edges if e.is_boundary); bm2.free()
print(f"boundary edges {before} -> {after}; new faces {len(new_faces)} ({flipped} flipped); magenta texel uv=({u:.4f},{v:.4f})")
bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
bpy.ops.object.shade_smooth()
bpy.ops.wm.obj_export(filepath=OBJ_OUT, export_selected_objects=True, export_triangulated_mesh=True,
                      export_materials=False, export_normals=True, export_uv=True)
print("faces", len(obj.data.polygons), "written", OBJ_OUT)
