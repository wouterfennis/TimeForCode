#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Smoke test for the TimeForCode local stack.

.DESCRIPTION
    Proves the stack is up and wired, in about a dozen checks. Feature-level
    behaviour (GitHub token handling, project lifecycle, role checks) is
    covered by the Reqnroll specifications, not here.

        1. Auth API, IdP Mock and Donation API respond
        2. Auth API OIDC discovery publishes issuer and JWKS
        3. Login round trip issues access and refresh cookies
        4. The JWT audiences include the Donation API
        5. The Auth API accepts its own JWT
        6. The Donation API accepts the Auth API JWT and stores a project (Mongo write)
        7. The stored project is read back (Mongo read)
        8. The data protection key ring is stored in the database (needs podman or, with -ContainerCli, docker)
        9. The front-end serves the SPA on / and on a deep link, and its built script
       10. A missing front-end asset returns 404 instead of the SPA
       11. Every front-end response carries the security headers
       12. The Reporting API rejects an anonymous call, and reaches the Donation API with the caller's JWT (a non-member gets the Donation API's 403 relayed, not 401 or 502)

.PARAMETER AuthApiBaseUrl
    Base URL of the Authorization API. Default: http://localhost:8080

.PARAMETER IdpMockBaseUrl
    Base URL of the Identity Provider Mock. Default: http://localhost:8081

.PARAMETER DonationApiBaseUrl
    Base URL of the Donation API. Default: http://localhost:8082

.PARAMETER ReportingApiBaseUrl
    Base URL of the Reporting API. Default: http://localhost:8083

.PARAMETER ClientRedirectUri
    Front-end origin the login redirects back to (must be a configured valid redirect URI). No front-end needs to run. Default: http://localhost:8084

.PARAMETER FrontendBaseUrl
    Base URL of the front-end (the SPA served by nginx). Default: http://localhost:8084

.PARAMETER ContainerCli
    Container CLI used to inspect the Mongo container (check 8). Default: podman, and the check is skipped when it is missing. CI passes docker explicitly, and then a missing CLI fails the check.

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
    [string] $ReportingApiBaseUrl = "http://localhost:8083",
    [string] $ClientRedirectUri    = "http://localhost:8084",
    [string] $FrontendBaseUrl      = "http://localhost:8084",
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

function Test-SpaPage([string[]] $response) {
    return (Get-StatusCode $response) -eq 200 -and ($response -join "`n").Contains('<div id="root">')
}

function Test-SameOriginCsp([string] $policy) {
    # Same origin only: default-src 'self', no framing, and no source other than 'self' or 'none'
    $directives = @($policy -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $sources    = @($directives | ForEach-Object { $_ -split '\s+' | Select-Object -Skip 1 })
    return ($directives -contains "default-src 'self'") -and
           ($directives -contains "frame-ancestors 'none'") -and
           -not ($sources | Where-Object { $_ -notin "'self'", "'none'" })
}

function Get-SecurityHeaderProblems([string[]] $response) {
    $problems = @()
    $csp = Get-HeaderValue $response "Content-Security-Policy"
    if (-not (Test-SameOriginCsp $csp)) { $problems += "Content-Security-Policy='$csp'" }
    $nosniff = Get-HeaderValue $response "X-Content-Type-Options"
    if ($nosniff -ne "nosniff") { $problems += "X-Content-Type-Options='$nosniff'" }
    $referrer = Get-HeaderValue $response "Referrer-Policy"
    if ($referrer -notin "no-referrer", "same-origin", "strict-origin", "strict-origin-when-cross-origin") {
        $problems += "Referrer-Policy='$referrer'"
    }
    return $problems
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
    if ($audList -contains $expectedAuthAud -and $audList -contains $DonationApiBaseUrl -and $audList -contains $ReportingApiBaseUrl -and $claims.sub) {
        Write-Pass "JWT audiences include Auth API, Donation API and Reporting API, subject present"
    } else {
        Write-Fail "JWT audiences or subject incorrect" "Got: $($audList -join ', '), expected '$expectedAuthAud', '$DonationApiBaseUrl' and '$ReportingApiBaseUrl'"
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

    # Reporting API: anonymous is refused; an authenticated non-member of a real organization gets the
    # Donation API's 403 relayed, which proves JWT validation, token forwarding and the Donation call.
    $reportBase = "$ReportingApiBaseUrl/api/v1/reporting/donororganization"
    $sAnon = [int](curl -s -o /dev/null -w "%{http_code}" "$reportBase/000000000000000000000000/impact-report" 2>&1 | Select-Object -Last 1)
    if ($sAnon -eq 401) { Write-Pass "Reporting API refuses an anonymous call: GET impact-report -> 401" }
    else                { Write-Fail "Reporting API anonymous call" "Status=$sAnon (expected 401)" }

    $orgBody = @{ name = "smoke-org-" + [guid]::NewGuid().ToString("N").Substring(0, 12) } | ConvertTo-Json -Compress
    $r7      = curl -si -H $authHeader -H "Content-Type: application/json" --data-raw $orgBody "$DonationApiBaseUrl/api/v1/donororganization" 2>&1
    $json7   = $r7 | Select-String '^\{' | Select-Object -First 1
    $orgId   = if ($json7) { ($json7.ToString() | ConvertFrom-Json).id } else { $null }
    if ((Get-StatusCode $r7) -eq 201 -and $orgId) {
        $sReport = [int](curl -s -o /dev/null -w "%{http_code}" -H $authHeader "$reportBase/$orgId/impact-report" 2>&1 | Select-Object -Last 1)
        if ($sReport -eq 403) { Write-Pass "Reporting API forwards the JWT to the Donation API: GET impact-report as non-member -> 403" }
        else                  { Write-Fail "Reporting API to Donation API round trip" "Status=$sReport (expected 403 for a non-member)" }
    } else {
        Write-Fail "Donation API POST /donororganization with the Auth JWT" "Status=$(Get-StatusCode $r7)"
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
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $ContainerCli exec mongo mongosh -u root -p example --authenticationDatabase admin AuthorizationServer --quiet --eval 'db.DataProtectionKeyDocument.countDocuments({})' 2>&1
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    $keys = ($output | ForEach-Object { "$_".Trim() } | Where-Object { $_ -match '^\d+$' } | Select-Object -Last 1)
    if ($keys -and [int]$keys -ge 1) { Write-Pass "Data protection key ring is stored in the database ($keys key(s))" }
    else                           { Write-Fail "No data protection key in the database" "mongosh said: $($output | Select-Object -Last 1)" }
} elseif ($PSBoundParameters.ContainsKey("ContainerCli")) {
    Write-Fail "Container CLI '$ContainerCli' was requested but is not available"
} else {
    Write-Output "  [SKIP] $ContainerCli not available, database not inspected"
}

# ---------------------------------------------------------------------------
# 5. Front-end (SPA served by nginx)
# ---------------------------------------------------------------------------

Write-Section "5. Front-end"

$rootPage = curl -si "$FrontendBaseUrl/" 2>&1

if ((Get-StatusCode $rootPage) -eq 0) {
    Write-Fail "Front-end reachable" "No response from $FrontendBaseUrl (is the front-end container running?)"
} else {
    $deepLink     = curl -si "$FrontendBaseUrl/projects/123" 2>&1
    $missingAsset = curl -si "$FrontendBaseUrl/assets/missing.js" 2>&1
    $assetPath    = if (($rootPage -join "`n") -match 'src="(/assets/[^"]+\.js)"') { $Matches[1] } else { $null }
    $builtAsset   = if ($assetPath) { curl -si "$FrontendBaseUrl$assetPath" 2>&1 } else { @() }

    if (-not $assetPath) {
        Write-Fail "Front-end SPA" "No script src=""/assets/*.js"" found in / (did the Vite output change?)"
    } elseif ((Test-SpaPage $rootPage) -and (Test-SpaPage $deepLink) -and ((Get-StatusCode $builtAsset) -eq 200)) {
        Write-Pass "Front-end serves the SPA on / and /projects/123, and its script $assetPath"
    } else {
        Write-Fail "Front-end SPA" "/=$(Get-StatusCode $rootPage), /projects/123=$(Get-StatusCode $deepLink), script '$assetPath'=$(Get-StatusCode $builtAsset)"
    }

    $sMissing = Get-StatusCode $missingAsset
    if ($sMissing -eq 404) { Write-Pass "Missing asset /assets/missing.js -> 404, no SPA fallback" }
    else                   { Write-Fail "Missing asset /assets/missing.js" "Status=$sMissing (expected 404)" }

    $frontendResponses = [ordered]@{ "/" = $rootPage; "/projects/123" = $deepLink; "script" = $builtAsset; "/assets/missing.js" = $missingAsset }
    $headerProblems = @(foreach ($entry in $frontendResponses.GetEnumerator()) {
        Get-SecurityHeaderProblems $entry.Value | ForEach-Object { "$($entry.Key) $_" }
    })
    if ($headerProblems.Count -eq 0) { Write-Pass "Security headers (CSP, nosniff, Referrer-Policy) on all $($frontendResponses.Count) front-end responses" }
    else                             { Write-Fail "Front-end security headers" ($headerProblems -join "; ") }
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
