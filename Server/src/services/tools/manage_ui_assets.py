"""
High-level MCP tools for UI Toolkit workflow in Unity.

This module provides AI-friendly tools for creating and managing
UI Toolkit UXML/USS files, including controller script generation.
"""
import asyncio
from typing import Annotated, Any, Literal

from fastmcp import Context
from mcp.types import ToolAnnotations

from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from services.tools.preflight import preflight
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry


@mcp_for_unity_tool(
    description=(
        "Manage UI Toolkit UXML and USS files.\n\n"
        "Actions:\n"
        "- get_uxml_structure: Parse and return the element hierarchy of a UXML file\n"
        "- add_visual_element: Add a new UI element to an existing UXML file\n"
        "- remove_visual_element: Remove an element by name from UXML\n"
        "- modify_visual_element: Change attributes, classes, or text of an element\n"
        "- add_style_rule: Add a new USS rule to a stylesheet\n"
        "- modify_style_rule: Update properties of an existing USS rule\n"
        "- get_uss_rules: List all rules in a USS file\n"
        "- get_element_styles: Get classes and inline styles for an element"
    ),
    annotations=ToolAnnotations(
        title="Manage UI Toolkit",
        destructiveHint=True,
    ),
)
async def manage_ui_toolkit(
    ctx: Context,
    action: Annotated[
        Literal[
            "get_uxml_structure",
            "add_visual_element",
            "remove_visual_element",
            "modify_visual_element",
            "add_style_rule",
            "modify_style_rule",
            "get_uss_rules",
            "get_element_styles",
        ],
        "Action to perform on UXML/USS files."
    ],
    path: Annotated[str, "Path to the UXML or USS file (relative to Assets/)."],
    element_name: Annotated[str, "Name of the element to target or create."] | None = None,
    element_type: Annotated[
        str,
        "Type of VisualElement to create (e.g., 'Button', 'Label', 'VisualElement', 'ScrollView')."
    ] | None = None,
    parent_name: Annotated[str, "Name of parent element to add child to."] | None = None,
    attributes: Annotated[
        dict[str, Any],
        "Attributes to set on the element (e.g., {'tooltip': 'Click me', 'focusable': 'true'})."
    ] | None = None,
    classes: Annotated[str, "USS class names to assign (space or comma-separated)."] | None = None,
    selector: Annotated[str, "USS selector (e.g., '.my-class', '#my-id', 'Button')."] | None = None,
    style_properties: Annotated[
        dict[str, str],
        "USS properties as dict (e.g., {'background-color': '#333', 'padding': '10px'})."
    ] | None = None,
    text: Annotated[str, "Text content for Label, Button, or Toggle elements."] | None = None,
    insert_index: Annotated[int, "Insert position index within parent (-1 for end)."] | None = None,
) -> dict[str, Any]:
    """Manage UI Toolkit UXML and USS files in Unity."""
    unity_instance = get_unity_instance_from_context(ctx)

    # Preflight check for compilation state
    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    # Build parameters for the Unity C# handler
    import json
    params_dict: dict[str, Any] = {
        "action": action,
        "path": path,
    }

    if element_name is not None:
        params_dict["elementName"] = element_name
    if element_type is not None:
        params_dict["elementType"] = element_type
    if parent_name is not None:
        params_dict["parentName"] = parent_name
    if attributes is not None:
        params_dict["attributes"] = json.dumps(attributes)
    if classes is not None:
        params_dict["classes"] = classes
    if selector is not None:
        params_dict["selector"] = selector
    if style_properties is not None:
        params_dict["styleProperties"] = json.dumps(style_properties)
    if text is not None:
        params_dict["text"] = text
    if insert_index is not None:
        params_dict["insertIndex"] = insert_index

    loop = asyncio.get_running_loop()
    result = await send_with_unity_instance(
        async_send_command_with_retry, unity_instance, "manage_ui_toolkit", params_dict, loop=loop
    )
    return result if isinstance(result, dict) else {"success": False, "message": str(result)}


@mcp_for_unity_tool(
    description=(
        "Create paired UXML and USS files for UI Toolkit interfaces.\n\n"
        "This creates a basic UI document with a root VisualElement and linked stylesheet.\n"
        "Use manage_ui_toolkit to modify the files after creation."
    ),
    annotations=ToolAnnotations(
        title="Create UI Toolkit Files",
        destructiveHint=True,
    ),
)
async def create_ui_toolkit_files(
    ctx: Context,
    base_name: Annotated[str, "Base file name (without extension) for the UXML/USS pair."],
    folder: Annotated[str, "Target folder under Assets/ (default: Assets/UI)."] = "Assets/UI",
    root_element_name: Annotated[str, "Name for the root VisualElement (default: 'root')."] = "root",
    overwrite: Annotated[bool, "Set true to overwrite existing files."] = False,
) -> dict[str, Any]:
    """Create paired UXML and USS files for UI Toolkit."""
    unity_instance = get_unity_instance_from_context(ctx)

    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    params_dict = {
        "folder": folder,
        "baseName": base_name,
        "rootElementName": root_element_name,
        "overwrite": overwrite,
    }

    loop = asyncio.get_running_loop()
    result = await send_with_unity_instance(
        async_send_command_with_retry, unity_instance, "create_ui_toolkit_files", params_dict, loop=loop
    )
    return result if isinstance(result, dict) else {"success": False, "message": str(result)}


@mcp_for_unity_tool(
    description=(
        "Generate a C# controller script for a UI Toolkit UIDocument.\n\n"
        "Creates a MonoBehaviour with:\n"
        "- UIDocument reference field\n"
        "- OnEnable/OnDisable lifecycle methods\n"
        "- Query methods for specified element names\n"
        "- Optional event handler stubs"
    ),
    annotations=ToolAnnotations(
        title="Create UI Controller Script",
        destructiveHint=True,
    ),
)
async def create_ui_controller_script(
    ctx: Context,
    class_name: Annotated[str, "Name of the C# class (must be valid C# identifier)."],
    path: Annotated[str, "Path for the script file (relative to Assets/, without .cs extension)."] = "Assets/Scripts",
    uxml_path: Annotated[str, "Path to the associated UXML file for element queries."] | None = None,
    element_names: Annotated[
        list[str],
        "List of element names to generate query fields for (e.g., ['submitButton', 'nameField'])."
    ] | None = None,
    namespace_name: Annotated[str, "C# namespace for the class."] | None = None,
    add_event_handlers: Annotated[
        bool,
        "Generate event handler methods for Button and TextField elements."
    ] = True,
) -> dict[str, Any]:
    """Generate a C# controller script for a UI Toolkit UIDocument."""
    unity_instance = get_unity_instance_from_context(ctx)

    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    # Generate the C# script content
    elements = element_names or []

    # Build using statements
    usings = [
        "using UnityEngine;",
        "using UnityEngine.UIElements;",
    ]

    # Build field declarations for UI elements
    fields = []
    queries = []
    event_registrations = []
    event_unregistrations = []
    event_handlers = []

    for elem_name in elements:
        # Infer element type from common naming conventions
        elem_type = "VisualElement"
        handler_type = None

        lower_name = elem_name.lower()
        if "button" in lower_name or lower_name.endswith("btn"):
            elem_type = "Button"
            handler_type = "clicked"
        elif "field" in lower_name or "input" in lower_name:
            elem_type = "TextField"
            handler_type = "RegisterValueChangedCallback"
        elif "label" in lower_name or "text" in lower_name:
            elem_type = "Label"
        elif "toggle" in lower_name or "checkbox" in lower_name:
            elem_type = "Toggle"
            handler_type = "RegisterValueChangedCallback"
        elif "slider" in lower_name:
            elem_type = "Slider"
            handler_type = "RegisterValueChangedCallback"
        elif "scroll" in lower_name:
            elem_type = "ScrollView"
        elif "container" in lower_name or "panel" in lower_name:
            elem_type = "VisualElement"

        # Generate field
        field_name = f"_{elem_name}"
        fields.append(f"    private {elem_type} {field_name};")

        # Generate query
        queries.append(f'        {field_name} = _root.Q<{elem_type}>("{elem_name}");')

        # Generate event handlers if requested
        if add_event_handlers and handler_type:
            handler_method = f"On{elem_name[0].upper()}{elem_name[1:]}"
            if handler_type == "clicked":
                event_registrations.append(f"        {field_name}.clicked += {handler_method};")
                event_unregistrations.append(f"        {field_name}.clicked -= {handler_method};")
                event_handlers.append(f"""
    private void {handler_method}()
    {{
        // TODO: Handle {elem_name} click
        Debug.Log("{elem_name} clicked");
    }}""")
            elif handler_type == "RegisterValueChangedCallback":
                value_type = "string" if elem_type == "TextField" else ("bool" if elem_type == "Toggle" else "float")
                event_registrations.append(f"        {field_name}.RegisterValueChangedCallback({handler_method});")
                event_unregistrations.append(f"        {field_name}.UnregisterValueChangedCallback({handler_method});")
                event_handlers.append(f"""
    private void {handler_method}(ChangeEvent<{value_type}> evt)
    {{
        // TODO: Handle {elem_name} value change
        Debug.Log($"{elem_name} changed: {{evt.newValue}}");
    }}""")

    # Build the script content
    script_lines = usings + [""]

    if namespace_name:
        script_lines.append(f"namespace {namespace_name}")
        script_lines.append("{")
        indent = "    "
    else:
        indent = ""

    script_lines.append(f"{indent}/// <summary>")
    script_lines.append(f"{indent}/// Controller for UI Toolkit document.")
    if uxml_path:
        script_lines.append(f"{indent}/// Associated UXML: {uxml_path}")
    script_lines.append(f"{indent}/// </summary>")
    script_lines.append(f"{indent}public class {class_name} : MonoBehaviour")
    script_lines.append(f"{indent}{{")

    # UIDocument field
    script_lines.append(f"{indent}    [SerializeField]")
    script_lines.append(f"{indent}    private UIDocument _document;")
    script_lines.append("")
    script_lines.append(f"{indent}    private VisualElement _root;")

    # Element fields
    if fields:
        script_lines.append("")
        for field in fields:
            script_lines.append(f"{indent}{field}")

    # OnEnable
    script_lines.append("")
    script_lines.append(f"{indent}    private void OnEnable()")
    script_lines.append(f"{indent}    {{")
    script_lines.append(f"{indent}        if (_document == null)")
    script_lines.append(f"{indent}            _document = GetComponent<UIDocument>();")
    script_lines.append("")
    script_lines.append(f"{indent}        _root = _document.rootVisualElement;")

    if queries:
        script_lines.append("")
        script_lines.append(f"{indent}        // Query UI elements")
        for query in queries:
            script_lines.append(f"{indent}{query}")

    if event_registrations:
        script_lines.append("")
        script_lines.append(f"{indent}        // Register event handlers")
        for reg in event_registrations:
            script_lines.append(f"{indent}{reg}")

    script_lines.append(f"{indent}    }}")

    # OnDisable
    if event_unregistrations:
        script_lines.append("")
        script_lines.append(f"{indent}    private void OnDisable()")
        script_lines.append(f"{indent}    {{")
        for unreg in event_unregistrations:
            script_lines.append(f"{indent}{unreg}")
        script_lines.append(f"{indent}    }}")

    # Event handlers
    for handler in event_handlers:
        for line in handler.split("\n"):
            script_lines.append(f"{indent}{line}")

    script_lines.append(f"{indent}}}")

    if namespace_name:
        script_lines.append("}")

    script_content = "\n".join(script_lines) + "\n"

    # Use manage_script to create the file
    params_dict = {
        "action": "create",
        "name": class_name,
        "path": path,
        "contents": script_content,
        "scriptType": "MonoBehaviour",
    }
    if namespace_name:
        params_dict["namespace"] = namespace_name

    loop = asyncio.get_running_loop()
    result = await send_with_unity_instance(
        async_send_command_with_retry, unity_instance, "manage_script", params_dict, loop=loop
    )

    if isinstance(result, dict):
        result["generatedElements"] = elements
        result["eventHandlersGenerated"] = len(event_handlers)

    return result if isinstance(result, dict) else {"success": False, "message": str(result)}


@mcp_for_unity_tool(
    description=(
        "Build a complete UI from a high-level description.\n\n"
        "Creates UXML structure, USS styles, and optionally a controller script.\n"
        "Supports common UI patterns like forms, dialogs, menus, and lists."
    ),
    annotations=ToolAnnotations(
        title="Build UI Component",
        destructiveHint=True,
    ),
)
async def build_ui_component(
    ctx: Context,
    name: Annotated[str, "Name for the UI component (used for file names and root element)."],
    component_type: Annotated[
        Literal["form", "dialog", "menu", "list", "panel", "custom"],
        "Type of UI component to create."
    ],
    folder: Annotated[str, "Target folder under Assets/ (default: Assets/UI)."] = "Assets/UI",
    elements: Annotated[
        list[dict[str, Any]],
        "List of elements to add. Each dict has: type, name, text (optional), classes (optional)."
    ] | None = None,
    styles: Annotated[
        dict[str, dict[str, str]],
        "Style rules as dict of selector -> properties dict."
    ] | None = None,
    create_controller: Annotated[bool, "Also generate a C# controller script."] = False,
    controller_namespace: Annotated[str, "Namespace for the controller class."] | None = None,
) -> dict[str, Any]:
    """Build a complete UI component from description."""
    unity_instance = get_unity_instance_from_context(ctx)

    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    results = {"steps": []}
    loop = asyncio.get_running_loop()

    # Step 1: Create base UXML/USS files
    create_params = {
        "folder": folder,
        "baseName": name,
        "rootElementName": "root",
        "overwrite": True,
    }

    create_result = await send_with_unity_instance(
        async_send_command_with_retry, unity_instance, "create_ui_toolkit_files", create_params, loop=loop
    )
    results["steps"].append({"action": "create_files", "result": create_result})

    if not create_result.get("success", False):
        return {"success": False, "message": "Failed to create base files", "details": results}

    uxml_path = create_result.get("data", {}).get("uxmlPath", f"{folder}/{name}.uxml")
    uss_path = create_result.get("data", {}).get("ussPath", f"{folder}/{name}.uss")

    # Step 2: Add default styles based on component type
    default_styles = _get_default_styles(component_type)
    if styles:
        default_styles.update(styles)

    import json
    for selector, props in default_styles.items():
        style_params = {
            "action": "add_style_rule",
            "path": uss_path,
            "selector": selector,
            "styleProperties": json.dumps(props),
        }
        style_result = await send_with_unity_instance(
            async_send_command_with_retry, unity_instance, "manage_ui_toolkit", style_params, loop=loop
        )
        results["steps"].append({"action": f"add_style_{selector}", "result": style_result})

    # Step 3: Add container element for the component type
    container_name = f"{name}-container"
    container_class = f"{component_type}-container"

    container_params = {
        "action": "add_visual_element",
        "path": uxml_path,
        "elementName": container_name,
        "elementType": "VisualElement",
        "parentName": "root",
        "classes": container_class,
    }
    container_result = await send_with_unity_instance(
        async_send_command_with_retry, unity_instance, "manage_ui_toolkit", container_params, loop=loop
    )
    results["steps"].append({"action": "add_container", "result": container_result})

    # Step 4: Add specified elements
    element_names = []
    if elements:
        for elem in elements:
            elem_type = elem.get("type", "VisualElement")
            elem_name = elem.get("name")
            elem_text = elem.get("text")
            elem_classes = elem.get("classes")

            if not elem_name:
                continue

            element_names.append(elem_name)

            elem_params: dict[str, Any] = {
                "action": "add_visual_element",
                "path": uxml_path,
                "elementName": elem_name,
                "elementType": elem_type,
                "parentName": container_name,
            }
            if elem_text:
                elem_params["text"] = elem_text
            if elem_classes:
                elem_params["classes"] = elem_classes

            elem_result = await send_with_unity_instance(
                async_send_command_with_retry, unity_instance, "manage_ui_toolkit", elem_params, loop=loop
            )
            results["steps"].append({"action": f"add_element_{elem_name}", "result": elem_result})

    # Step 5: Create controller if requested
    controller_path = None
    if create_controller:
        controller_class = f"{name}Controller"
        controller_path = f"Assets/Scripts/UI"

        # Call create_ui_controller_script via manage_script
        script_result = await create_ui_controller_script(
            ctx=ctx,
            class_name=controller_class,
            path=controller_path,
            uxml_path=uxml_path,
            element_names=element_names,
            namespace_name=controller_namespace,
            add_event_handlers=True,
        )
        results["steps"].append({"action": "create_controller", "result": script_result})
        if script_result.get("success", False):
            controller_path = script_result.get("data", {}).get("path")

    return {
        "success": True,
        "message": f"Created {component_type} UI component '{name}'",
        "data": {
            "uxmlPath": uxml_path,
            "ussPath": uss_path,
            "controllerPath": controller_path,
            "elementCount": len(element_names),
            "styleRuleCount": len(default_styles),
        },
        "steps": results["steps"],
    }


def _get_default_styles(component_type: str) -> dict[str, dict[str, str]]:
    """Get default USS styles for a component type."""
    base_styles: dict[str, dict[str, str]] = {
        ":root": {
            "flex-grow": "1",
            "padding": "10px",
        },
    }

    if component_type == "form":
        base_styles.update({
            ".form-container": {
                "flex-direction": "column",
                "padding": "20px",
            },
            ".form-container > *": {
                "margin-bottom": "10px",
            },
            ".form-container Label": {
                "margin-bottom": "4px",
                "-unity-font-style": "bold",
            },
            ".form-container Button": {
                "height": "30px",
                "margin-top": "10px",
            },
        })
    elif component_type == "dialog":
        base_styles.update({
            ".dialog-container": {
                "background-color": "rgba(40, 40, 40, 0.95)",
                "border-radius": "8px",
                "padding": "20px",
                "min-width": "300px",
                "align-self": "center",
            },
            ".dialog-container Label": {
                "margin-bottom": "15px",
                "white-space": "normal",
            },
        })
    elif component_type == "menu":
        base_styles.update({
            ".menu-container": {
                "flex-direction": "column",
                "background-color": "rgba(30, 30, 30, 0.9)",
                "padding": "5px",
            },
            ".menu-container Button": {
                "background-color": "rgba(0, 0, 0, 0)",
                "border-width": "0",
                "-unity-text-align": "middle-left",
                "padding": "8px 12px",
            },
            ".menu-container Button:hover": {
                "background-color": "rgba(255, 255, 255, 0.1)",
            },
        })
    elif component_type == "list":
        base_styles.update({
            ".list-container": {
                "flex-direction": "column",
            },
            ".list-container > *": {
                "padding": "8px",
                "border-bottom-width": "1px",
                "border-bottom-color": "rgba(255, 255, 255, 0.1)",
            },
        })
    elif component_type == "panel":
        base_styles.update({
            ".panel-container": {
                "background-color": "rgba(50, 50, 50, 0.8)",
                "border-radius": "4px",
                "padding": "15px",
            },
        })

    return base_styles
