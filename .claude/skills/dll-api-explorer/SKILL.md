---
name: dll-api-explorer
description: "Inspects the public API of a compiled .NET assembly (.dll) via reflection-only metadata when source or docs are unavailable. Use when: checking types, overloads or signatures of a dependency or NuGet package before using it."
argument-hint: "Path to the .dll (or NuGet package id/version) and, optionally, the specific namespace or type to inspect"
---

# dll-api-explorer Skill

This skill inspects a compiled .NET assembly to discover its public API surface — namespaces, types, members, and signatures — without relying on the vendor's documentation. Follow every step in order.

---

## Trigger Conditions

Invoke this skill when:

- A handler, controller, or infrastructure class needs to call a third-party or BCL API and the exact member names, overloads, or generic constraints are unknown
- A NuGet package is being evaluated before it is added as a dependency
- A reference assembly (e.g. under `dotnet packs`) needs to be checked for a type that may or may not exist in the target framework version
- Vendor documentation is missing, outdated, or does not match the installed package version

---

## Required Inputs

| Input | Source |
| --- | --- |
| Assembly path (`.dll`) | Located in Step 1 |
| Trust level of the assembly | Known first-party/BCL/NuGet package vs. unknown/untrusted binary — determines which approach in Step 3 to use |
| (Optional) Specific type or namespace of interest | Narrows the output in Steps 4–6 |

---

## Expected Outputs

- A list of public namespaces and types in the assembly
- For a specific type: its public constructors, methods (with parameter/return types and generic constraints), properties, and events
- Any available `<summary>` documentation extracted from the companion XML doc file
- (When needed) Decompiled method bodies showing actual implementation behaviour

---

## Step 1 — Locate the Assembly

Find the `.dll` before doing anything else. Common locations in this repo's environment:

| Source | Typical path |
| --- | --- |
| NuGet package cache | `$env:USERPROFILE\.nuget\packages\<package-id-lowercase>\<version>\lib\<tfm>\*.dll` |
| .NET reference assemblies (BCL surface for a target framework) | `C:\Program Files\dotnet\packs\<PackName>\<version>\ref\<tfm>\*.dll` |
| .NET shared runtime (implementation assemblies) | `C:\Program Files\dotnet\shared\<PackName>\<version>\*.dll` |
| This solution's own build output | `src\<Module>\<Project>\bin\<Config>\<tfm>\*.dll` |

```powershell
# Example: find every copy of an assembly across the NuGet cache and reference packs
Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages","C:\Program Files\dotnet\packs" -Recurse -Filter "Newtonsoft.Json.dll" -ErrorAction SilentlyContinue |
    Select-Object FullName, LastWriteTime
```

Prefer the reference-assembly (`ref\`) copy when you only need the public API shape — it contains signatures only (no IL bodies) and cannot be executed, which is both faster and safer to inspect.

---

## Step 2 — Choose the Inspection Depth

Pick the lightest approach that answers the question. Do not decompile an entire assembly just to check whether one method exists.

| Need | Approach | Go to |
| --- | --- | --- |
| Just the human-readable description of a type/member | XML doc companion file | Step 5 |
| Names, signatures, overloads, generic constraints — nothing more | Reflection (trusted assembly) or `MetadataLoadContext` (untrusted assembly) | Step 3–4 |
| Actual implementation / behaviour of a method | Decompilation | Step 6 |

---

## Steps 3-6 — Commands

Code and examples are in [reference.md](reference.md):

- **Step 3** — enumerate types: `Add-Type`/`LoadFrom` for trusted assemblies (3a); `MetadataLoadContext` from a scratch console project for unknown ones (3b).
- **Step 4** — drill into one type: constructors, methods, properties (`.ToString()` prints readable signatures).
- **Step 5** — search the companion XML doc file with `Select-String` (`T:`/`M:`/`P:`/`F:`/`E:` prefixes).
- **Step 6** — decompile with `ilspycmd` only when signatures do not answer the question.
- Error handling table: end of reference.md.

## Notes

- Never load an assembly of unknown provenance with `Assembly.Load`/`LoadFrom`/`Add-Type` — those execute code. `MetadataLoadContext` and decompilers (`ilspycmd`) are the safe choices when trust is uncertain.
- Prefer the `ref\` reference-assembly copy over the `shared\` runtime copy when only the public API shape is needed — reference assemblies contain no executable IL bodies.
- Clean up any scratch console projects, `.tmp-dll-inspect`, or `.tmp-decompiled` folders created during investigation; do not leave them in the repository working tree.
- This skill only discovers an API — it does not decide how to use it. Once the signature/behaviour is understood, continue with the normal Implementation workflow (tests first, then production code).
