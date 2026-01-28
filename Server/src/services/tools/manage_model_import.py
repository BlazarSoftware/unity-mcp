"""
Defines the manage_model_import tool for configuring Unity model import settings.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from mcp.types import ToolAnnotations

from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from services.tools.utils import parse_json_payload, coerce_bool, coerce_float, normalize_properties
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry


@mcp_for_unity_tool(
    description="Configure model import settings for FBX/OBJ/GLTF files. Actions: get_settings, set_settings, configure_humanoid, validate_rig, get_bone_mapping.",
    annotations=ToolAnnotations(
        title="Manage Model Import",
        destructiveHint=True,
    ),
)
async def manage_model_import(
    ctx: Context,
    action: Annotated[Literal[
        "get_settings",
        "set_settings",
        "configure_humanoid",
        "validate_rig",
        "get_bone_mapping"
    ], "Action to perform."],

    # Common
    asset_path: Annotated[str,
                          "Path to model asset (Assets/...)"] | None = None,

    # set_settings
    global_scale: Annotated[float | None,
                            "Global import scale factor"] = None,
    mesh_compression: Annotated[Literal["Off", "Low", "Medium", "High"] | None,
                                "Mesh compression level"] = None,
    import_animation: Annotated[bool | None,
                                "Whether to import animations"] = None,
    material_import_mode: Annotated[Literal["None", "ImportViaMaterialDescription", "ImportViaStandardMaterial"] | None,
                                    "How to handle material import"] = None,
    read_write_enabled: Annotated[bool | None,
                                  "Enable read/write access to mesh data"] = None,
    generate_colliders: Annotated[bool | None,
                                  "Generate mesh colliders"] = None,
    optimize_mesh: Annotated[bool | None,
                             "Optimize mesh for GPU"] = None,

    # configure_humanoid
    animation_type: Annotated[Literal["None", "Legacy", "Generic", "Human"] | None,
                              "Animation rig type"] = None,
    avatar_definition: Annotated[Literal["CreateFromThisModel", "CopyFromOther"] | None,
                                 "How to create avatar definition"] = None,
    source_avatar_path: Annotated[str | None,
                                  "Path to source avatar when using CopyFromOther"] = None,

    # Bone mapping for manual configuration
    bone_mapping: Annotated[dict[str, str] | None,
                            "Dict mapping Unity humanoid bone names to actual bone names in the model"] = None,

) -> dict[str, Any]:
    unity_instance = get_unity_instance_from_context(ctx)

    # Normalize bone_mapping
    bone_mapping_parsed, bone_err = normalize_properties(bone_mapping)
    if bone_err:
        return {"success": False, "message": bone_err}

    # Coerce booleans
    import_animation_val = coerce_bool(import_animation)
    read_write_enabled_val = coerce_bool(read_write_enabled)
    generate_colliders_val = coerce_bool(generate_colliders)
    optimize_mesh_val = coerce_bool(optimize_mesh)

    # Coerce float
    global_scale_val = coerce_float(global_scale)

    params_dict = {
        "action": action.lower(),
        "assetPath": asset_path,
        "globalScale": global_scale_val,
        "meshCompression": mesh_compression,
        "importAnimation": import_animation_val,
        "materialImportMode": material_import_mode,
        "readWriteEnabled": read_write_enabled_val,
        "generateColliders": generate_colliders_val,
        "optimizeMesh": optimize_mesh_val,
        "animationType": animation_type,
        "avatarDefinition": avatar_definition,
        "sourceAvatarPath": source_avatar_path,
        "boneMapping": bone_mapping_parsed
    }

    # Remove None values
    params_dict = {k: v for k, v in params_dict.items() if v is not None}

    result = await send_with_unity_instance(
        async_send_command_with_retry,
        unity_instance,
        "manage_model_import",
        params_dict,
    )

    return result if isinstance(result, dict) else {"success": False, "message": str(result)}
