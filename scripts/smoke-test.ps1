#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke test for the TimeForCode local stack.

.DESCRIPTION
    Verifies that all services are reachable and that the full OAuth2
    authentication flow works end-to-end:

        Browser -> Website -> Auth API -> IdP Mock -> Auth API callback
                           -> JWT issued -> /api/v1/user returns user profile

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

function Invoke-Step([string] $url, [hashtable] $curlArgs = @{}) {
    # Returns the raw response headers + body as an array of strings
    $curlParameters = @("-si", "--max-redirs", "0") + ($curlArgs.GetEnumerator() |
        ForEach-Object { $_.Key; if ($null -ne $_.Value) { $_.Value } })
    return curl @curlParameters $url 2>&1
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
# Section 1: Service availability
# ---------------------------------------------------------------------------

Write-Section "1. Service availability"

$checks = @(
    @{ Label = "Auth API OIDC discovery  ($AuthApiBaseUrl)";    Url = "$AuthApiBaseUrl/.well-known/openid-configuration" }
    @{ Label = "IdP Mock OIDC discovery  ($IdpMockBaseUrl)";    Url = "$IdpMockBaseUrl/.well-known/openid-configuration" }
    @{ Label = "Donation API             ($DonationApiBaseUrl)"; Url = "$DonationApiBaseUrl/api/v1/project" }
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

$discoveredAuthIssuer = ""
try {
    $authOidc = curl -s "$AuthApiBaseUrl/.well-known/openid-configuration" 2>&1 | ConvertFrom-Json
    if ($authOidc.issuer) {
        $discoveredAuthIssuer = $authOidc.issuer
        if ($authOidc.issuer -eq $AuthApiBaseUrl) {
            Write-Pass "Auth API issuer matches ($($authOidc.issuer))"
        } else {
            # The docker-compose configures TokenCreationOptions__Issuer with the
            # internal Docker service hostname. This is expected in a local stack.
            Write-Pass "Auth API issuer present ($($authOidc.issuer)) [NOTE: differs from external base URL '$AuthApiBaseUrl']"
        }
    } else {
        Write-Fail "Auth API issuer missing from OIDC document"
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
    $loginUrl    = "$AuthApiBaseUrl/api/v1/authentication/login?IdentityProvider=Github&RedirectUri=$redirectUri"

    $r1 = curl -si --max-redirs 0 -c $cookieJar $loginUrl 2>&1
    $s1 = Get-StatusCode $r1
    $idpRedirect = Get-HeaderValue $r1 "Location"

    if ($s1 -eq 302 -and $idpRedirect -match [regex]::Escape($IdpMockBaseUrl)) {
        Write-Pass "Step 3.1: Auth API login -> 302 to IdP Mock"
    } else {
        Write-Fail "Step 3.1: Auth API login" "Status=$s1, Location=$idpRedirect"
    }

    # -- Step 3.2: IdP Mock authorize page -> 200 HTML login form ----------
    $r2 = curl -si --max-redirs 0 $idpRedirect 2>&1
    $s2 = Get-StatusCode $r2
    $htmlBody = ($r2 -join "`n")

    if ($s2 -eq 200 -and $htmlBody -match 'action="/login/oauth/confirm"') {
        Write-Pass "Step 3.2: IdP Mock authorize -> 200 HTML form"
    } else {
        Write-Fail "Step 3.2: IdP Mock authorize" "Status=$s2 (expected 200 with confirm form)"
    }

    # -- Step 3.2b: Submit the authorize form -> 302 with auth code --------
    $formState       = Get-HtmlInputValue $htmlBody "state"
    $formRedirectUri = Get-HtmlInputValue $htmlBody "redirect_uri"
    $formClientId    = Get-HtmlInputValue $htmlBody "client_id"
    $formScope       = Get-HtmlInputValue $htmlBody "scope"

    $r2b = curl -si --max-redirs 0 `
        --data-urlencode "state=$formState" `
        --data-urlencode "redirect_uri=$formRedirectUri" `
        --data-urlencode "client_id=$formClientId" `
        --data-urlencode "scope=$formScope" `
        "$IdpMockBaseUrl/login/oauth/confirm" 2>&1
    $s2b = Get-StatusCode $r2b
    $callbackRedirect = Get-HeaderValue $r2b "Location"

    if ($s2b -eq 302 -and $callbackRedirect -match "callback" -and $callbackRedirect -match "code=") {
        Write-Pass "Step 3.2b: IdP Mock confirm -> 302 to Auth API callback with code"
    } else {
        Write-Fail "Step 3.2b: IdP Mock confirm" "Status=$s2b, Location=$callbackRedirect"
    }

    # -- Step 3.3: Auth API callback -> JWT cookies + redirect to Website --
    $r3 = curl -si --max-redirs 0 -c $cookieJar -b $cookieJar $callbackRedirect 2>&1
    $s3 = Get-StatusCode $r3
    $websiteRedirect = Get-HeaderValue $r3 "Location"
    $hasAccessToken  = $null -ne ($r3 | Select-String "Set-Cookie:.*AccessToken")
    $hasRefreshToken = $null -ne ($r3 | Select-String "Set-Cookie:.*RefreshToken")

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

            if ($discoveredAuthIssuer -and $claims.iss -eq $discoveredAuthIssuer) {
                Write-Pass "Step 3.4: JWT issuer matches OIDC discovery ($($claims.iss))"
            } elseif (-not $discoveredAuthIssuer -and $claims.iss) {
                Write-Pass "Step 3.4: JWT issuer present ($($claims.iss)) [OIDC discovery issuer unavailable for comparison]"
            } else {
                Write-Fail "Step 3.4: JWT issuer wrong" "Got '$($claims.iss)', OIDC discovery says '$discoveredAuthIssuer'"
            }

            $audList = @($claims.aud)
            # Audience uses the discovered internal issuer for the Auth API and the
            # external base URL for the Donation API (as configured in docker-compose).
            $expectedAuthAud = if ($discoveredAuthIssuer) { $discoveredAuthIssuer } else { $AuthApiBaseUrl }
            if ($audList -contains $expectedAuthAud -and $audList -contains $DonationApiBaseUrl) {
                Write-Pass "Step 3.4: JWT audiences include Auth API and Donation API"
            } else {
                Write-Fail "Step 3.4: JWT audiences incorrect" "Got: $($audList -join ', '), expected '$expectedAuthAud' and '$DonationApiBaseUrl'"
            }

            if ($claims.sub) { Write-Pass "Step 3.4: JWT subject (user id) present ($($claims.sub))" }
            else              { Write-Fail "Step 3.4: JWT subject missing" }

            # -- Step 3.5: Call protected /api/v1/user with JWT ---------------
            $bearer  = $tokenJson.Token
            $r5      = curl -si -H "Authorization: Bearer $bearer" "$AuthApiBaseUrl/api/v1/user" 2>&1
            $s5      = Get-StatusCode $r5
            $userBody = $r5 | Select-String '^\{' | Select-Object -First 1

            if ($s5 -eq 200) {
                Write-Pass "Step 3.5: GET /api/v1/user -> 200 OK"
            } else {
                Write-Fail "Step 3.5: GET /api/v1/user" "Status=$s5"
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
