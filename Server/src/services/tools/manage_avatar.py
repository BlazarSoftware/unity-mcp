"""
Defines the manage_avatar tool for configuring Unity humanoid avatars.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from mcp.types import ToolAnnotations

from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from services.tools.utils import normalize_properties
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry


@mcp_for_unity_tool(
    description="Configure Unity humanoid avatar bone mappings. Actions: get_mapping, set_mapping, validate, auto_map, get_required_bones.",
    annotations=ToolAnnotations(
        title="Manage Avatar",
        destructiveHint=True,
    ),
)
async def manage_avatar(
    ctx: Context,
    action: Annotated[Literal[
        "get_mapping",
        "set_mapping",
        "validate",
        "auto_map",
        "get_required_bones"
    ], "Action to perform."],

    # Common
    asset_path: Annotated[str,
                          "Path to model asset with humanoid avatar (Assets/...)"] | None = None,

    # set_mapping
    bone_mapping: Annotated[dict[str, str] | None,
                            """Dict mapping Unity humanoid bone names to model bone names.
                            Keys are Unity names like 'Hips', 'Spine', 'LeftUpperArm', etc.
                            Values are the actual bone names in the model."""] = None,

    # auto_map
    naming_convention: Annotated[Literal["auto", "mixamo", "unreal", "unity", "rigify"] | None,
                                 "Hint for bone naming convention to help auto-mapping"] = None,

) -> dict[str, Any]:
    unity_instance = get_unity_instance_from_context(ctx)

    # Normalize bone_mapping
    bone_mapping_parsed, bone_err = normalize_properties(bone_mapping)
    if bone_err:
        return {"success": False, "message": bone_err}

    params_dict = {
        "action": action.lower(),
        "assetPath": asset_path,
        "boneMapping": bone_mapping_parsed,
        "namingConvention": naming_convention
    }

    # Remove None values
    params_dict = {k: v for k, v in params_dict.items() if v is not None}

    result = await send_with_unity_instance(
        async_send_command_with_retry,
        unity_instance,
        "manage_avatar",
        params_dict,
    )

    return result if isinstance(result, dict) else {"success": False, "message": str(result)}
