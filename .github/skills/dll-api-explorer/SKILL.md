---
name: dll-api-explorer
description: 'Inspects the public API surface of an external .NET library directly from its compiled assembly (.dll) when source code, IntelliSense, or documentation is unavailable or incomplete. Uses reflection-only metadata loading, XML doc comments, and static decompilation to enumerate namespaces, types, members, and signatures. Use when: understanding a third-party or reference assembly API, checking what overloads/members exist on a type, inspecting a NuGet package before adding it, finding types in a .dll, "what''s in this dll", "how do I call X from this library", no source available for a dependency.'
argument-hint: 'Path to the .dll (or NuGet package id/version) and, optionally, the specific namespace or type to inspect'
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
|-------|--------|
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
|--------|-------------|
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
|------|----------|-------|
| Just the human-readable description of a type/member | XML doc companion file | Step 5 |
| Names, signatures, overloads, generic constraints — nothing more | Reflection (trusted assembly) or `MetadataLoadContext` (untrusted assembly) | Step 3–4 |
| Actual implementation / behaviour of a method | Decompilation | Step 6 |

---

## Step 3 — Enumerate Types and Members via Reflection

### 3a. Trusted assemblies (BCL, reference packs, NuGet packages you intend to add, this solution's own output)

These are safe to load into a throwaway PowerShell session with `Add-Type`. Loading for reflection purposes only executes module/static initializers if a type is actually instantiated or a static member accessed — listing types and members does not trigger this.

```powershell
Add-Type -Path "C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref\10.0.8\ref\net10.0\Microsoft.Extensions.Identity.Core.dll"

$asm = [System.Reflection.Assembly]::LoadFrom("C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref\10.0.8\ref\net10.0\Microsoft.Extensions.Identity.Core.dll")

# List every public type
$asm.GetExportedTypes() | Sort-Object FullName | Select-Object FullName, IsInterface, IsAbstract
```

### 3b. Unknown or untrusted assemblies (source not verified — e.g. a downloaded `.dll` with no matching NuGet package)

Do not `Add-Type`/`Assembly.LoadFrom` a binary you don't trust — it can run arbitrary code at load time. Use `System.Reflection.MetadataLoadContext` instead, which parses metadata only and never executes the assembly. Run it from a scratch console project so the package can be restored:

```powershell
dotnet new console -o .tmp-dll-inspect
cd .tmp-dll-inspect
dotnet add package System.Reflection.MetadataLoadContext
```

```csharp
using System.Reflection;

var dllPath = @"C:\path\to\Unknown.dll";
var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
var resolver = new PathAssemblyResolver(
    Directory.GetFiles(runtimeDir, "*.dll").Append(dllPath));

using var mlc = new MetadataLoadContext(resolver);
var assembly = mlc.LoadFromAssemblyPath(dllPath);

foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName))
{
    Console.WriteLine(type.FullName);
}
```

Run with `dotnet run`, then delete the scratch project once done.

---

## Step 4 — Drill Into a Specific Type

Once the type of interest is known, list its members:

```powershell
$type = $asm.GetType("Microsoft.Extensions.Identity.Core.UserPasskeyInfo")

$type.GetConstructors() | ForEach-Object { $_.ToString() }
$type.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::Static) |
    Where-Object { -not $_.IsSpecialName } |
    ForEach-Object { $_.ToString() }
$type.GetProperties() | ForEach-Object { $_.ToString() }
```

`.ToString()` on a `MethodInfo`/`ConstructorInfo`/`PropertyInfo` prints a readable signature (e.g. `Boolean TryGetValue(System.String, Int32 ByRef)`), which is usually enough to write the calling code correctly.

---

## Step 5 — Correlate With XML Documentation Comments

Most assemblies ship a companion `<AssemblyName>.xml` file next to the `.dll` containing `<summary>`/`<param>`/`<returns>` doc comments. Search it directly instead of opening it in an editor:

```powershell
Select-String -Path "C:\Program Files\dotnet\packs\Microsoft.AspNetCore.App.Ref\10.0.8\ref\net10.0\Microsoft.Extensions.Identity.Core.xml" `
    -Pattern "UserPasskeyInfo|IUserPasskeyStore" -Context 0,3
```

The `name` attribute prefix indicates the member kind: `T:` type, `M:` method, `P:` property, `F:` field, `E:` event. Matching the prefix plus the fully-qualified name from Step 3/4 pinpoints the exact doc block.

---

## Step 6 — Decompile for Implementation Detail (Only When Needed)

When signatures alone don't answer the question (e.g. you need to know exactly how a method mutates its inputs, or what exceptions it can throw), decompile with `ilspycmd`. This is a static tool — it reads IL and prints C#-like source without ever executing the target assembly, so it is safe even for untrusted binaries:

```powershell
dotnet tool install -G ilspycmd 2>$null   # already installed if this errors
ilspycmd --help                            # confirm flags for the installed version

# Decompile the whole assembly to a folder of .cs files
ilspycmd "C:\path\to\Library.dll" -o .tmp-decompiled

# Or decompile a single type
ilspycmd "C:\path\to\Library.dll" -t "Namespace.TypeName"
```

Delete `.tmp-decompiled` (and any scratch console project from Step 3b) once you've extracted what you need — do not commit decompiled output into the repository.

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `Add-Type`/`LoadFrom` throws `FileNotFoundException` for a dependency | The assembly has unresolved references; either add the missing dependency `.dll` to the same folder or switch to `MetadataLoadContext` with a `PathAssemblyResolver` that includes the runtime + NuGet package directories |
| `Assembly.LoadFrom` throws `BadImageFormatException` | Architecture/target-framework mismatch (e.g. inspecting a .NET Framework assembly from a .NET SDK PowerShell host) — use `ilspycmd` instead, it does not need to load the assembly into the running process |
| No companion `.xml` doc file exists | Skip Step 5; rely on signatures from Step 3–4 and, if needed, decompiled bodies from Step 6 |
| `ilspycmd` not found | Run `dotnet tool install -G ilspycmd` (or `--update-tool` if an old version is installed) |

---

## Notes

- Never load an assembly of unknown provenance with `Assembly.Load`/`LoadFrom`/`Add-Type` — those execute code. `MetadataLoadContext` and decompilers (`ilspycmd`) are the safe choices when trust is uncertain.
- Prefer the `ref\` reference-assembly copy over the `shared\` runtime copy when only the public API shape is needed — reference assemblies contain no executable IL bodies.
- Clean up any scratch console projects, `.tmp-dll-inspect`, or `.tmp-decompiled` folders created during investigation; do not leave them in the repository working tree.
- This skill only discovers an API — it does not decide how to use it. Once the signature/behaviour is understood, continue with the normal Implementation workflow (tests first, then production code).
