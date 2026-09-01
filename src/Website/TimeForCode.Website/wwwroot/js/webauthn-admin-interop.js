// Admin-specific WebAuthn passkey interop. Own file/module — isolated from the GitHub OAuth flow.
// Talks directly to the Authorization API from the browser (not proxied through the Website's server),
// mirroring how the GitHub login flow also has the browser call the Authorization API directly, so the
// resulting HttpOnly cookies are set on the shared origin.

function base64UrlToBuffer(base64Url) {
    const padding = "=".repeat((4 - (base64Url.length % 4)) % 4);
    const base64 = (base64Url + padding).replace(/-/g, "+").replace(/_/g, "/");
    const raw = atob(base64);
    const buffer = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) {
        buffer[i] = raw.charCodeAt(i);
    }
    return buffer.buffer;
}

function bufferToBase64Url(buffer) {
    const bytes = new Uint8Array(buffer);
    let str = "";
    for (let i = 0; i < bytes.byteLength; i++) {
        str += String.fromCharCode(bytes[i]);
    }
    return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function decodeCreationOptions(optionsJson) {
    const options = JSON.parse(optionsJson);
    options.challenge = base64UrlToBuffer(options.challenge);
    options.user.id = base64UrlToBuffer(options.user.id);
    if (options.excludeCredentials) {
        options.excludeCredentials = options.excludeCredentials.map(c => ({ ...c, id: base64UrlToBuffer(c.id) }));
    }
    return options;
}

function decodeRequestOptions(optionsJson) {
    const options = JSON.parse(optionsJson);
    options.challenge = base64UrlToBuffer(options.challenge);
    if (options.allowCredentials) {
        options.allowCredentials = options.allowCredentials.map(c => ({ ...c, id: base64UrlToBuffer(c.id) }));
    }
    return options;
}

function encodeAttestationCredential(credential) {
    return JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64Url(credential.rawId),
        type: credential.type,
        response: {
            clientDataJSON: bufferToBase64Url(credential.response.clientDataJSON),
            attestationObject: bufferToBase64Url(credential.response.attestationObject)
        }
    });
}

function encodeAssertionCredential(credential) {
    return JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64Url(credential.rawId),
        type: credential.type,
        response: {
            clientDataJSON: bufferToBase64Url(credential.response.clientDataJSON),
            authenticatorData: bufferToBase64Url(credential.response.authenticatorData),
            signature: bufferToBase64Url(credential.response.signature),
            userHandle: credential.response.userHandle ? bufferToBase64Url(credential.response.userHandle) : null
        }
    });
}

async function registerAsync(baseUri, bootstrapSecret) {
    const optionsResponse = await fetch(`${baseUri}/api/v1/admin-authentication/registration/options`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ bootstrapSecret })
    });

    if (!optionsResponse.ok) {
        return false;
    }

    const { optionsJson } = await optionsResponse.json();
    const publicKey = decodeCreationOptions(optionsJson);
    const credential = await navigator.credentials.create({ publicKey });
    const credentialJson = encodeAttestationCredential(credential);

    const completeResponse = await fetch(`${baseUri}/api/v1/admin-authentication/registration`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ bootstrapSecret, credentialJson })
    });

    return completeResponse.ok;
}

async function authenticateAsync(baseUri) {
    const optionsResponse = await fetch(`${baseUri}/api/v1/admin-authentication/authentication/options`, {
        credentials: "include"
    });

    if (!optionsResponse.ok) {
        return false;
    }

    const { optionsJson } = await optionsResponse.json();
    const publicKey = decodeRequestOptions(optionsJson);
    const credential = await navigator.credentials.get({ publicKey });
    const credentialJson = encodeAssertionCredential(credential);

    const completeResponse = await fetch(`${baseUri}/api/v1/admin-authentication/authentication`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ credentialJson })
    });

    return completeResponse.ok;
}

// Tries the ongoing authentication ceremony first; falls back to one-time registration when no admin
// credential has been claimed yet (the authentication/options call fails in that case).
export async function loginOrRegisterAsAdminAsync(baseUri) {
    if (await authenticateAsync(baseUri)) {
        return true;
    }

    const bootstrapSecret = window.prompt("Enter the admin bootstrap secret:");
    if (!bootstrapSecret) {
        return false;
    }

    return await registerAsync(baseUri, bootstrapSecret);
}
