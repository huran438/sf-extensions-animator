---
name: sf-extensions-animator
description: Use when adding SFAnimatorParameter Inspector selectors for Animator parameter names or hashes.
---

# sf-extensions-animator

Read [the package guide](references/package-readme.md) and [installed source](SOURCE.md).

Apply SFAnimatorParameter to a serialized string or integer field and provide the exact serialized Animator field name. Optionally filter by AnimatorControllerParameterType.

The referenced field must hold an Animator. The drawer searches the root serialized object and a root _value fallback, not arbitrary nested objects. Verify references before diagnosing missing parameter options.

String fields store parameter names and integer fields store nameHash. Missing parameters show their stored values. Select another parameter or None to change them. A multi-object selection needs a shared Animator reference for the dropdown.

Runtime animation calls and validation belong to the application. Keep editor integrations out of player assemblies.

Verify string/hash selection, type filters, missing Animator references and renamed parameters in the consuming Unity Editor.
