<#
.SYNOPSIS
    Builds the Postman collection of every endpoint from the OpenAPI documents of the three APIs.

.DESCRIPTION
    Converts the OpenAPI document that each API project commits (src/<Module>/TimeForCode.<Module>.Api/TimeForCode.<Module>.Api.json)
    with the pinned openapi-to-postmanv2 converter, then merges the three collections into
    docs/postman/TimeForCode-all-endpoints.postman_collection.json (scripts/postman-merge.mjs). Nothing is written by hand: when an API
    changes, build the solution (that rewrites the OpenAPI documents) and run this script again.

    Needs Node and network access to the npm registry the first time the converter is fetched.

.EXAMPLE
    pwsh scripts/generate-postman-collection.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# At least two weeks old when pinned (supply chain); raise it deliberately.
$converter = 'openapi-to-postmanv2@6.4.1'
$converterOptions = 'folderStrategy=Tags,requestNameSource=URL,requestParametersResolution=Schema,exampleParametersResolution=Schema,parametersResolution=Schema'

$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'docs/postman/TimeForCode-all-endpoints.postman_collection.json'
$work = Join-Path ([System.IO.Path]::GetTempPath()) "timeforcode-postman-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $work | Out-Null

$apis = @(
    @{ Folder = 'Authorization API'; Variable = 'authorizationUrl'; Module = 'Authorization' },
    @{ Folder = 'Donation API'; Variable = 'donationUrl'; Module = 'Donation' },
    @{ Folder = 'Reporting API'; Variable = 'reportingUrl'; Module = 'Reporting' }
)

try {
    $merge = @()
    foreach ($api in $apis) {
        $spec = Join-Path $root "src/$($api.Module)/TimeForCode.$($api.Module).Api/TimeForCode.$($api.Module).Api.json"
        if (-not (Test-Path $spec)) {
            throw "OpenAPI document not found: $spec. Build the solution first (dotnet build TimeForCode.sln)."
        }

        $converted = Join-Path $work "$($api.Module).json"
        npx --yes --package $converter openapi2postmanv2 -s $spec -o $converted -p -O $converterOptions
        if ($LASTEXITCODE -ne 0) {
            throw "The conversion of $spec failed."
        }

        $merge += "$($api.Folder)|$($api.Variable)|$converted"
    }

    New-Item -ItemType Directory -Force (Split-Path $output) | Out-Null
    node (Join-Path $PSScriptRoot 'postman-merge.mjs') $output @merge
    if ($LASTEXITCODE -ne 0) {
        throw 'Merging the collections failed.'
    }
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
