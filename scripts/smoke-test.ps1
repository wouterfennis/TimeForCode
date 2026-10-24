#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke test for the TimeForCode local stack.

.DESCRIPTION
    Proves the stack is up and wired, in about ten checks. Feature-level
    behaviour (GitHub token handling, project lifecycle, role checks) is
    covered by the Reqnroll specifications, not here.

        1. Auth API, IdP Mock and Donation API respond
        2. Auth API OIDC discovery publishes issuer and JWKS
        3. Login round trip issues access and refresh cookies
        4. The JWT audiences include the Donation API
        5. The Auth API accepts its own JWT
        6. The Donation API accepts the Auth API JWT and stores a project (Mongo write)
        7. The stored project is read back (Mongo read)
        8. The data protection key ring is stored in the database (needs podman)

.PARAMETER AuthApiBaseUrl
    Base URL of the Authorization API. Default: http://localhost:8080

.PARAMETER IdpMockBaseUrl
    Base URL of the Identity Provider Mock. Default: http://localhost:8081

.PARAMETER DonationApiBaseUrl
    Base URL of the Donation API. Default: http://localhost:8082

.PARAMETER ClientRedirectUri
    Front-end origin the login redirects back to (must be a configured valid redirect URI). No front-end needs to run. Default: http://localhost:8084

.PARAMETER ContainerCli
    Container CLI used to inspect the Mongo container (check 8). Default: podman. CI passes docker.

.EXAMPLE
    .\scripts\smoke-test.ps1

.EXAMPLE
    .\scripts\smoke-test.ps1 -AuthApiBaseUrl http://localhost:8080
#>
[CmdletBinding()]
param(
    [string] $AuthApiBaseUrl    = "http://localhost:8080",
    [string] $IdpMockBaseUrl    = "http://localhost:8081",
    [string] $DonationApiBaseUrl = "http://localhost:8082",
    [string] $ClientRedirectUri    = "http://localhost:8084",
    [string] $ContainerCli         = "podman"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

$passed = 0
$failed = 0

function Write-Pass([string] $label) {
    Write-Output "  [PASS] $label"
    $script:passed++
}

function Write-Fail([string] $label, [string] $detail = "") {
    Write-Output "  [FAIL] $label"
    if ($detail) { Write-Output "         $detail" }
    $script:failed++
}

function Write-Section([string] $title) {
    Write-Output ""
    Write-Output $title
    Write-Output ("-" * $title.Length)
}

function Get-StatusCode([string[]] $response) {
    $line = $response | Select-String "^HTTP/\d" | Select-Object -Last 1
    if ($line -match "HTTP/\S+\s+(\d+)") { return [int]$Matches[1] }
    return 0
}

function Get-HeaderValue([string[]] $response, [string] $header) {
    $line = $response | Select-String "(?i)^${header}:\s*(.+)$" | Select-Object -First 1
    if ($line -match "(?i)^${header}:\s*(.+)$") { return $Matches[1].Trim() }
    return $null
}

function Get-HtmlInputValue([string] $html, [string] $name) {
    if ($html -match ('name="' + $name + '"\s+value="([^"]*)"')) {
        return [System.Net.WebUtility]::HtmlDecode($Matches[1])
    }
    return ""
}

function Decode-JwtPayload([string] $token) {
    $segment = $token.Split('.')[1]
    $padded  = $segment + ('=' * ((4 - $segment.Length % 4) % 4))
    $bytes   = [System.Convert]::FromBase64String($padded)
    return [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
}

# ---------------------------------------------------------------------------
# 1. Service availability
# ---------------------------------------------------------------------------

Write-Section "1. Service availability"

$checks = @(
    @{ Label = "Auth API OIDC discovery  ($AuthApiBaseUrl)";    Url = "$AuthApiBaseUrl/.well-known/openid-configuration" }
    @{ Label = "IdP Mock OIDC discovery  ($IdpMockBaseUrl)";    Url = "$IdpMockBaseUrl/.well-known/openid-configuration" }
    @{ Label = "Donation API             ($DonationApiBaseUrl)"; Url = "$DonationApiBaseUrl/api/v1/project" }
)

foreach ($check in $checks) {
    try {
        $resp = curl -si --max-redirs 5 -o /dev/null -w "%{http_code}" $check.Url 2>&1
        $code = [int]($resp | Select-Object -Last 1)
        if ($code -ge 200 -and $code -lt 400) {
            Write-Pass "$($check.Label) -> $code"
        } else {
            Write-Fail "$($check.Label) -> $code"
        }
    } catch {
        Write-Fail "$($check.Label) -> unreachable: $_"
    }
}

# ---------------------------------------------------------------------------
# 2. Auth API OIDC discovery
# ---------------------------------------------------------------------------

Write-Section "2. OIDC discovery"

$discoveredAuthIssuer = ""
try {
    $authOidc = curl -s "$AuthApiBaseUrl/.well-known/openid-configuration" 2>&1 | ConvertFrom-Json
    if ($authOidc.issuer -and $authOidc.jwks_uri) {
        # The docker-compose configures the issuer with the internal Docker service
        # hostname, so it may legitimately differ from the external base URL.
        $discoveredAuthIssuer = $authOidc.issuer
        Write-Pass "Auth API publishes issuer ($($authOidc.issuer)) and jwks_uri"
    } else {
        Write-Fail "Auth API OIDC document lacks issuer or jwks_uri"
    }
} catch {
    Write-Fail "Auth API OIDC document could not be parsed: $_"
}

# ---------------------------------------------------------------------------
# 3. Login round trip and cross-service JWT wiring
# ---------------------------------------------------------------------------

Write-Section "3. Login round trip and JWT wiring"

$cookieJar = [System.IO.Path]::GetTempFileName()
try {
    # Auth API login -> IdP Mock authorize form -> confirm -> Auth API callback
    $redirectUri = [System.Uri]::EscapeDataString($ClientRedirectUri)
    $r1 = curl -si --max-redirs 0 -c $cookieJar "$AuthApiBaseUrl/api/v1/authentication/login?IdentityProvider=Github&RedirectUri=$redirectUri" 2>&1
    $idpRedirect = Get-HeaderValue $r1 "Location"
    if ((Get-StatusCode $r1) -ne 302 -or $idpRedirect -notmatch [regex]::Escape($IdpMockBaseUrl)) {
        throw "login did not redirect to the IdP Mock (status $(Get-StatusCode $r1), location '$idpRedirect')"
    }

    $r2 = curl -si --max-redirs 0 $idpRedirect 2>&1
    $htmlBody = ($r2 -join "`n")
    if ((Get-StatusCode $r2) -ne 200 -or $htmlBody -notmatch 'action="/login/oauth/confirm"') {
        throw "IdP Mock authorize did not return the confirm form (status $(Get-StatusCode $r2))"
    }

    $r2b = curl -si --max-redirs 0 `
        --data-urlencode "state=$(Get-HtmlInputValue $htmlBody 'state')" `
        --data-urlencode "redirect_uri=$(Get-HtmlInputValue $htmlBody 'redirect_uri')" `
        --data-urlencode "client_id=$(Get-HtmlInputValue $htmlBody 'client_id')" `
        --data-urlencode "scope=$(Get-HtmlInputValue $htmlBody 'scope')" `
        "$IdpMockBaseUrl/login/oauth/confirm" 2>&1
    $callbackRedirect = Get-HeaderValue $r2b "Location"
    if ((Get-StatusCode $r2b) -ne 302 -or $callbackRedirect -notmatch "callback" -or $callbackRedirect -notmatch "code=") {
        throw "IdP Mock confirm did not redirect to the callback with a code (status $(Get-StatusCode $r2b))"
    }

    $r3 = curl -si --max-redirs 0 -c $cookieJar -b $cookieJar $callbackRedirect 2>&1
    $frontendRedirect = Get-HeaderValue $r3 "Location"
    $hasAccessToken  = $null -ne ($r3 | Select-String "Set-Cookie:.*AccessToken")
    $hasRefreshToken = $null -ne ($r3 | Select-String "Set-Cookie:.*RefreshToken")
    if ((Get-StatusCode $r3) -eq 302 -and $frontendRedirect -match [regex]::Escape($ClientRedirectUri) -and $hasAccessToken -and $hasRefreshToken) {
        Write-Pass "Login round trip -> 302 to the client redirect URI with access and refresh cookies"
    } else {
        Write-Fail "Login round trip callback" "Status=$(Get-StatusCode $r3), Location=$frontendRedirect, access=$hasAccessToken, refresh=$hasRefreshToken"
    }

    $accessCookie = ($r3 | Select-String "Set-Cookie:\s*AccessToken=([^;]+)").Matches
    if ($accessCookie.Count -eq 0) { throw "AccessToken cookie not found, cannot continue" }
    $tokenJson  = [System.Uri]::UnescapeDataString($accessCookie[0].Groups[1].Value) | ConvertFrom-Json
    $bearer     = $tokenJson.Token
    $claims     = Decode-JwtPayload $bearer
    $authHeader = "Authorization: Bearer $bearer"

    # The Donation API validates tokens issued by the Auth API, so its audience must be in the JWT.
    $audList         = @($claims.aud)
    $expectedAuthAud = if ($discoveredAuthIssuer) { $discoveredAuthIssuer } else { $AuthApiBaseUrl }
    if ($audList -contains $expectedAuthAud -and $audList -contains $DonationApiBaseUrl -and $claims.sub) {
        Write-Pass "JWT audiences include Auth API and Donation API, subject present"
    } else {
        Write-Fail "JWT audiences or subject incorrect" "Got: $($audList -join ', '), expected '$expectedAuthAud' and '$DonationApiBaseUrl'"
    }

    $r5 = curl -si -H $authHeader "$AuthApiBaseUrl/api/v1/user" 2>&1
    if ((Get-StatusCode $r5) -eq 200 -and ($r5 | Select-String '"login"')) {
        Write-Pass "Auth API accepts its JWT: GET /api/v1/user -> 200 with profile"
    } else {
        Write-Fail "Auth API GET /api/v1/user" "Status=$(Get-StatusCode $r5)"
    }

    # Donation API accepts the Auth JWT: one Mongo-backed write, then read it back.
    $projectUrl   = "$DonationApiBaseUrl/api/v1/project"
    $repoName     = "smoke-" + [guid]::NewGuid().ToString("N").Substring(0, 12)
    $registerBody = @{ githubRepositoryUrl = "https://github.com/smoke-owner/$repoName" } | ConvertTo-Json -Compress
    $r6    = curl -si -H $authHeader -H "Content-Type: application/json" --data-raw $registerBody $projectUrl 2>&1
    $json6 = $r6 | Select-String '^\{' | Select-Object -First 1
    $projectId = if ($json6) { ($json6.ToString() | ConvertFrom-Json).projectId } else { $null }

    if ((Get-StatusCode $r6) -eq 201 -and $projectId) {
        Write-Pass "Donation API accepts the Auth JWT and stores a project: POST /project -> 201 ($projectId)"
        $sRead = [int](curl -s -o /dev/null -w "%{http_code}" -H $authHeader "$projectUrl/$projectId/manage" 2>&1 | Select-Object -Last 1)
        if ($sRead -eq 200) { Write-Pass "Stored project is read back: GET /project/{id}/manage -> 200" }
        else                { Write-Fail "Stored project read back" "Status=$sRead" }
    } else {
        Write-Fail "Donation API POST /project with the Auth JWT" "Status=$(Get-StatusCode $r6)"
    }
} catch {
    Write-Fail "Authentication flow aborted: $_"
} finally {
    Remove-Item $cookieJar -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------------------
# 4. Data protection key ring
# ---------------------------------------------------------------------------

Write-Section "4. Data protection key ring"

if (Get-Command $ContainerCli -ErrorAction SilentlyContinue) {
    $keys = & $ContainerCli exec mongo mongosh -u root -p example --authenticationDatabase admin AuthorizationServer --quiet --eval 'db.DataProtectionKeyDocument.countDocuments({})' 2>&1 | Select-Object -Last 1
    if ([int]"$keys".Trim() -ge 1) { Write-Pass "Data protection key ring is stored in the database ($keys key(s))" }
    else                           { Write-Fail "No data protection key in the database" "mongosh said: $keys" }
} else {
    Write-Output "  [SKIP] $ContainerCli not available, database not inspected"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "================================" -ForegroundColor White
$total = $passed + $failed
if ($failed -eq 0) {
    Write-Host "  Result: PASSED ($passed/$total)" -ForegroundColor Green
} else {
    Write-Host "  Result: FAILED ($failed/$total failed)" -ForegroundColor Red
}
Write-Host "================================" -ForegroundColor White
Write-Host ""

exit $failed
