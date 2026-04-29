#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke test for the TimeForCode local stack.

.DESCRIPTION
    Verifies that all services are reachable and that the full OAuth2
    authentication flow works end-to-end:

        Browser -> Website -> Auth API -> IdP Mock -> Auth API callback
                           -> JWT issued -> /api/user returns user profile

.PARAMETER AuthApiBaseUrl
    Base URL of the Authorization API. Default: http://localhost:8080

.PARAMETER IdpMockBaseUrl
    Base URL of the Identity Provider Mock. Default: http://localhost:8081

.PARAMETER DonationApiBaseUrl
    Base URL of the Donation API. Default: http://localhost:8082

.PARAMETER WebsiteBaseUrl
    Base URL of the Website. Default: http://localhost:8083

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
    [string] $WebsiteBaseUrl    = "http://localhost:8083"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

$passed = 0
$failed = 0

function Write-Pass([string] $label) {
    Write-Host "  [PASS] $label" -ForegroundColor Green
    $script:passed++
}

function Write-Fail([string] $label, [string] $detail = "") {
    Write-Host "  [FAIL] $label" -ForegroundColor Red
    if ($detail) { Write-Host "         $detail" -ForegroundColor DarkRed }
    $script:failed++
}

function Write-Section([string] $title) {
    Write-Host ""
    Write-Host $title -ForegroundColor Cyan
    Write-Host ("-" * $title.Length) -ForegroundColor DarkCyan
}

function Invoke-Step([string] $url, [hashtable] $curlArgs = @{}) {
    # Returns the raw response headers + body as an array of strings
    $args = @("-si", "--max-redirs", "0") + ($curlArgs.GetEnumerator() |
        ForEach-Object { $_.Key; if ($_.Value -ne $null) { $_.Value } })
    return curl @args $url 2>&1
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

function Decode-JwtPayload([string] $token) {
    $segment = $token.Split('.')[1]
    $padded  = $segment + ('=' * ((4 - $segment.Length % 4) % 4))
    $bytes   = [System.Convert]::FromBase64String($padded)
    return [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
}

# ---------------------------------------------------------------------------
# Section 1: Service availability
# ---------------------------------------------------------------------------

Write-Section "1. Service availability"

$checks = @(
    @{ Label = "Auth API OIDC discovery  ($AuthApiBaseUrl)";    Url = "$AuthApiBaseUrl/.well-known/openid-configuration" }
    @{ Label = "IdP Mock OIDC discovery  ($IdpMockBaseUrl)";    Url = "$IdpMockBaseUrl/.well-known/openid-configuration" }
    @{ Label = "Donation API             ($DonationApiBaseUrl)"; Url = "$DonationApiBaseUrl" }
    @{ Label = "Website                  ($WebsiteBaseUrl)";     Url = "$WebsiteBaseUrl" }
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
# Section 2: Auth API OIDC discovery content
# ---------------------------------------------------------------------------

Write-Section "2. OIDC discovery documents"

try {
    $authOidc = curl -s "$AuthApiBaseUrl/.well-known/openid-configuration" 2>&1 | ConvertFrom-Json
    if ($authOidc.issuer -eq $AuthApiBaseUrl) {
        Write-Pass "Auth API issuer matches ($($authOidc.issuer))"
    } else {
        Write-Fail "Auth API issuer mismatch" "Expected '$AuthApiBaseUrl', got '$($authOidc.issuer)'"
    }
    if ($authOidc.jwks_uri) {
        Write-Pass "Auth API jwks_uri present ($($authOidc.jwks_uri))"
    } else {
        Write-Fail "Auth API jwks_uri missing"
    }
} catch {
    Write-Fail "Auth API OIDC document could not be parsed: $_"
}

try {
    $idpOidc = curl -s "$IdpMockBaseUrl/.well-known/openid-configuration" 2>&1 | ConvertFrom-Json
    if ($idpOidc.issuer -eq $IdpMockBaseUrl) {
        Write-Pass "IdP Mock issuer matches ($($idpOidc.issuer))"
    } else {
        Write-Fail "IdP Mock issuer mismatch" "Expected '$IdpMockBaseUrl', got '$($idpOidc.issuer)'"
    }
} catch {
    Write-Fail "IdP Mock OIDC document could not be parsed: $_"
}

# ---------------------------------------------------------------------------
# Section 3: Full authentication flow
# ---------------------------------------------------------------------------

Write-Section "3. Authentication flow"

$cookieJar = [System.IO.Path]::GetTempFileName()
try {
    # -- Step 3.1: Login endpoint -> redirect to IdP Mock ------------------
    $redirectUri = [System.Uri]::EscapeDataString($WebsiteBaseUrl)
    $loginUrl    = "$AuthApiBaseUrl/api/authentication/login?IdentityProvider=Github&RedirectUri=$redirectUri"

    $r1 = curl -si --max-redirs 0 -c $cookieJar $loginUrl 2>&1
    $s1 = Get-StatusCode $r1
    $idpRedirect = Get-HeaderValue $r1 "Location"

    if ($s1 -eq 302 -and $idpRedirect -match [regex]::Escape($IdpMockBaseUrl)) {
        Write-Pass "Step 3.1: Auth API login -> 302 to IdP Mock"
    } else {
        Write-Fail "Step 3.1: Auth API login" "Status=$s1, Location=$idpRedirect"
    }

    # -- Step 3.2: IdP Mock -> redirect back to Auth API callback ----------
    $r2 = curl -si --max-redirs 0 $idpRedirect 2>&1
    $s2 = Get-StatusCode $r2
    $callbackRedirect = Get-HeaderValue $r2 "Location"

    if ($s2 -eq 302 -and $callbackRedirect -match "callback" -and $callbackRedirect -match "code=") {
        Write-Pass "Step 3.2: IdP Mock -> 302 to Auth API callback with code"
    } else {
        Write-Fail "Step 3.2: IdP Mock authorize" "Status=$s2, Location=$callbackRedirect"
    }

    # -- Step 3.3: Auth API callback -> JWT cookies + redirect to Website --
    $r3 = curl -si --max-redirs 0 -c $cookieJar -b $cookieJar $callbackRedirect 2>&1
    $s3 = Get-StatusCode $r3
    $websiteRedirect = Get-HeaderValue $r3 "Location"
    $hasAccessToken  = ($r3 | Select-String "Set-Cookie:.*AccessToken") -ne $null
    $hasRefreshToken = ($r3 | Select-String "Set-Cookie:.*RefreshToken") -ne $null

    if ($s3 -eq 302 -and $websiteRedirect -match [regex]::Escape($WebsiteBaseUrl)) {
        Write-Pass "Step 3.3: Auth API callback -> 302 to Website"
    } else {
        Write-Fail "Step 3.3: Auth API callback redirect" "Status=$s3, Location=$websiteRedirect"
    }
    if ($hasAccessToken)  { Write-Pass "Step 3.3: AccessToken cookie set" }
    else                  { Write-Fail "Step 3.3: AccessToken cookie missing" }
    if ($hasRefreshToken) { Write-Pass "Step 3.3: RefreshToken cookie set" }
    else                  { Write-Fail "Step 3.3: RefreshToken cookie missing" }

    # -- Step 3.4: Validate JWT claims ------------------------------------
    $cookieHeader = ($r3 | Select-String "Set-Cookie:\s*AccessToken=([^;]+)").Matches
    if ($cookieHeader.Count -gt 0) {
        try {
            $rawCookie  = $cookieHeader[0].Groups[1].Value
            $tokenJson  = [System.Uri]::UnescapeDataString($rawCookie) | ConvertFrom-Json
            $claims     = Decode-JwtPayload $tokenJson.Token

            if ($claims.iss -eq $AuthApiBaseUrl) {
                Write-Pass "Step 3.4: JWT issuer correct ($($claims.iss))"
            } else {
                Write-Fail "Step 3.4: JWT issuer wrong" "Got '$($claims.iss)', expected '$AuthApiBaseUrl'"
            }

            $audList = @($claims.aud)
            if ($audList -contains $AuthApiBaseUrl -and $audList -contains $DonationApiBaseUrl) {
                Write-Pass "Step 3.4: JWT audiences include Auth API and Donation API"
            } else {
                Write-Fail "Step 3.4: JWT audiences incorrect" "Got: $($audList -join ', ')"
            }

            if ($claims.sub) { Write-Pass "Step 3.4: JWT subject (user id) present ($($claims.sub))" }
            else              { Write-Fail "Step 3.4: JWT subject missing" }

            # -- Step 3.5: Call protected /api/user with JWT ---------------
            $bearer  = $tokenJson.Token
            $r5      = curl -si -H "Authorization: Bearer $bearer" "$AuthApiBaseUrl/api/user" 2>&1
            $s5      = Get-StatusCode $r5
            $userBody = $r5 | Select-String '^\{' | Select-Object -First 1

            if ($s5 -eq 200) {
                Write-Pass "Step 3.5: GET /api/user -> 200 OK"
            } else {
                Write-Fail "Step 3.5: GET /api/user" "Status=$s5"
            }

            if ($userBody -match '"login"') {
                $user = ($userBody.ToString()) | ConvertFrom-Json
                Write-Pass "Step 3.5: User profile returned (login='$($user.login)')"
            } else {
                Write-Fail "Step 3.5: User profile body unexpected" "$userBody"
            }

        } catch {
            Write-Fail "Step 3.4: JWT decode/validation failed: $_"
        }
    } else {
        Write-Fail "Step 3.4: Cannot validate JWT — AccessToken cookie not found"
    }

} catch {
    Write-Fail "Authentication flow aborted: $_"
} finally {
    Remove-Item $cookieJar -ErrorAction SilentlyContinue
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
