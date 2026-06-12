# Stand the flat-lying recall_notice envelope (and its detached letter) upright.
import bpy
import math

PATH = "D:/source/DefenseBudget/mods/SupplyChain/SupplyChain/models/recall_notice.obj"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=PATH)
obj = next(o for o in bpy.data.objects if o.type == 'MESH')
bpy.context.view_layer.objects.active = obj
obj.rotation_euler = (math.radians(90), 0, 0)
bpy.ops.object.transform_apply(rotation=True)
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.ops.wm.obj_export(
    filepath=PATH,
    export_selected_objects=True,
    export_triangulated_mesh=True,
    export_materials=False,
    export_normals=True,
    export_uv=True,
)
print("rotated and re-exported", PATH)
