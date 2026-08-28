/*
 * ============================================================================
 * WebGLGoogleSheets.jslib
 * ============================================================================
 *
 * Reads a PRIVATE Google Spreadsheet from a Unity WebGL build.
 * Works on https, on localhost, AND on plain http:// (insecure context),
 * because RS256 signing falls back to a pure-JS implementation when
 * window.crypto.subtle is unavailable.
 *
 * ----------------------------------------------------------------------------
 * WHY THE PREVIOUS VERSION CRASHED
 * ----------------------------------------------------------------------------
 * A .jslib is executed by the Emscripten compiler at BUILD time; only the
 * source text of each function passed to mergeInto() is copied into
 * Build.framework.js. Helpers living in a surrounding closure (an IIFE) are
 * never emitted -> "getAccessToken is not defined".
 *
 * Runtime helpers MUST be library members prefixed with '$', kept alive by
 * <functionName>__deps. That is the pattern used below.
 *
 * ----------------------------------------------------------------------------
 * AUTH SOURCES (no C# change needed - it all comes through the
 * serviceAccountJson string you already pass in)
 * ----------------------------------------------------------------------------
 * 1. Service-account JSON  (has client_email + private_key)
 *      -> signs a JWT locally and exchanges it for an access token.
 *         Good for test/dev. NOTE: the private key ships inside the build
 *         and is readable by anyone who opens the game.
 *
 * 2. {"access_token":"ya29...."}  or a bare "ya29...." string
 *      -> used directly as the Bearer token.
 *
 * 3. {"token_url":"https://your-backend/sheets-token"}   (optional "headers")
 *      -> GET/POST that URL, expects {"access_token":"...","expires_in":3600}.
 *         This is the recommended PRODUCTION path: the key stays on your
 *         server, the sheet stays private, the client only gets a short-lived
 *         read-only token.
 *
 * 4. {"proxy_url":"https://your-backend/sheets"}
 *      -> no token at all; every Sheets call is forwarded to your backend as
 *         <proxy_url>?path=<encoded google api path+query>. Your backend adds
 *         the Authorization header. Most secure option.
 *
 * ----------------------------------------------------------------------------
 * NOTES FOR http://
 * ----------------------------------------------------------------------------
 * - An http page CAN fetch https endpoints (mixed-content blocking only goes
 *   the other way), so googleapis.com calls work fine.
 * - crypto.subtle is absent on http -> the pure-JS signer below is used.
 *   Cost measured: ~10-40 ms per token, and the token is cached for its
 *   lifetime, so it happens once per session.
 * - BigInt is required (all browsers since 2018). If it is missing the error
 *   is reported cleanly back to Unity instead of throwing.
 * ============================================================================
 */

var GoogleSheetsLibrary = {

    // ========================================================================
    // Runtime helper object -> emitted as global "GSheetsHelper"
    // ========================================================================

    $GSheetsHelper: {

        _token: null,
        _tokenExpiry: 0,
        _proxyUrl: null,

        // --------------------------------------------------------------------
        // Unity callback
        // --------------------------------------------------------------------

        sendResponse: function (gameObjectName, requestId, success, error, data) {

            var payload;

            try {
                payload = JSON.stringify({
                    requestId: requestId,
                    success: success,
                    error: error || "",
                    dataJson: data ? JSON.stringify(data) : ""
                });
            }
            catch (stringifyError) {
                payload = JSON.stringify({
                    requestId: requestId,
                    success: false,
                    error: "Failed to serialize response: " + stringifyError,
                    dataJson: ""
                });
            }

            try {
                if (typeof SendMessage === "function") {
                    SendMessage(gameObjectName, "OnGoogleSheetsResponse", payload);
                }
                else if (typeof Module !== "undefined" &&
                    typeof Module.SendMessage === "function") {
                    Module.SendMessage(gameObjectName, "OnGoogleSheetsResponse", payload);
                }
                else if (typeof unityInstance !== "undefined" && unityInstance &&
                    typeof unityInstance.SendMessage === "function") {
                    unityInstance.SendMessage(gameObjectName, "OnGoogleSheetsResponse", payload);
                }
                else {
                    console.error("[GSheets] SendMessage unavailable; response dropped.");
                }
            }
            catch (sendError) {
                console.error("[GSheets] SendMessage threw", sendError);
            }
        },

        // --------------------------------------------------------------------
        // Base64 / UTF-8
        // --------------------------------------------------------------------

        utf8Bytes: function (str) {
            var out = [];
            for (var i = 0; i < str.length; i++) {
                var c = str.charCodeAt(i);
                if (c < 0x80) {
                    out.push(c);
                }
                else if (c < 0x800) {
                    out.push(0xc0 | (c >> 6), 0x80 | (c & 63));
                }
                else if (c >= 0xd800 && c <= 0xdbff && i + 1 < str.length) {
                    var c2 = str.charCodeAt(++i);
                    var cp = 0x10000 + ((c - 0xd800) << 10) + (c2 - 0xdc00);
                    out.push(0xf0 | (cp >> 18),
                        0x80 | ((cp >> 12) & 63),
                        0x80 | ((cp >> 6) & 63),
                        0x80 | (cp & 63));
                }
                else {
                    out.push(0xe0 | (c >> 12), 0x80 | ((c >> 6) & 63), 0x80 | (c & 63));
                }
            }
            return new Uint8Array(out);
        },

        base64UrlFromString: function (value) {
            return btoa(unescape(encodeURIComponent(value)))
                .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
        },

        base64UrlFromBytes: function (bytes) {
            var binary = "";
            var chunk = 0x8000;
            for (var i = 0; i < bytes.length; i += chunk) {
                binary += String.fromCharCode.apply(
                    null, bytes.subarray(i, i + chunk));
            }
            return btoa(binary)
                .replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
        },

        // --------------------------------------------------------------------
        // PEM -> DER
        // --------------------------------------------------------------------

        pemToDer: function (pem) {
            var body = String(pem)
                .replace(/\\r/g, "")
                .replace(/\\n/g, "\n")
                .replace(/-----[^-]+-----/g, "")
                .replace(/\s/g, "");

            if (!body) throw new Error("private_key is empty after PEM cleanup.");

            var binary = atob(body);
            var bytes = new Uint8Array(binary.length);
            for (var i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
            return bytes;
        },

        // --------------------------------------------------------------------
        // SHA-256 (pure JS)
        // --------------------------------------------------------------------

        sha256: function (bytes) {

            var K = [
                0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
                0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
                0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
                0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
                0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
                0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
                0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
                0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
            ];

            var h0 = 0x6a09e667, h1 = 0xbb67ae85, h2 = 0x3c6ef372, h3 = 0xa54ff53a,
                h4 = 0x510e527f, h5 = 0x9b05688c, h6 = 0x1f83d9ab, h7 = 0x5be0cd19;

            var len = bytes.length;
            var total = ((len + 9 + 63) >> 6) << 6;
            var padded = new Uint8Array(total);
            padded.set(bytes);
            padded[len] = 0x80;

            var bitLen = len * 8;
            var hi = Math.floor(bitLen / 4294967296);
            var lo = bitLen >>> 0;

            padded[total - 8] = (hi >>> 24) & 255;
            padded[total - 7] = (hi >>> 16) & 255;
            padded[total - 6] = (hi >>> 8) & 255;
            padded[total - 5] = hi & 255;
            padded[total - 4] = (lo >>> 24) & 255;
            padded[total - 3] = (lo >>> 16) & 255;
            padded[total - 2] = (lo >>> 8) & 255;
            padded[total - 1] = lo & 255;

            var w = new Uint32Array(64);

            function rotr(x, n) { return ((x >>> n) | (x << (32 - n))) >>> 0; }

            for (var offset = 0; offset < total; offset += 64) {

                for (var i = 0; i < 16; i++) {
                    w[i] = (padded[offset + i * 4] << 24) |
                        (padded[offset + i * 4 + 1] << 16) |
                        (padded[offset + i * 4 + 2] << 8) |
                        (padded[offset + i * 4 + 3]);
                }

                for (i = 16; i < 64; i++) {
                    var s0 = rotr(w[i - 15], 7) ^ rotr(w[i - 15], 18) ^ (w[i - 15] >>> 3);
                    var s1 = rotr(w[i - 2], 17) ^ rotr(w[i - 2], 19) ^ (w[i - 2] >>> 10);
                    w[i] = (w[i - 16] + s0 + w[i - 7] + s1) >>> 0;
                }

                var a = h0, b = h1, c = h2, d = h3, e = h4, f = h5, g = h6, h = h7;

                for (i = 0; i < 64; i++) {
                    var S1 = rotr(e, 6) ^ rotr(e, 11) ^ rotr(e, 25);
                    var ch = (e & f) ^ (~e & g);
                    var t1 = (h + S1 + ch + K[i] + w[i]) >>> 0;
                    var S0 = rotr(a, 2) ^ rotr(a, 13) ^ rotr(a, 22);
                    var maj = (a & b) ^ (a & c) ^ (b & c);
                    var t2 = (S0 + maj) >>> 0;

                    h = g; g = f; f = e;
                    e = (d + t1) >>> 0;
                    d = c; c = b; b = a;
                    a = (t1 + t2) >>> 0;
                }

                h0 = (h0 + a) >>> 0; h1 = (h1 + b) >>> 0;
                h2 = (h2 + c) >>> 0; h3 = (h3 + d) >>> 0;
                h4 = (h4 + e) >>> 0; h5 = (h5 + f) >>> 0;
                h6 = (h6 + g) >>> 0; h7 = (h7 + h) >>> 0;
            }

            var out = new Uint8Array(32);
            var hs = [h0, h1, h2, h3, h4, h5, h6, h7];

            for (var j = 0; j < 8; j++) {
                out[j * 4] = (hs[j] >>> 24) & 255;
                out[j * 4 + 1] = (hs[j] >>> 16) & 255;
                out[j * 4 + 2] = (hs[j] >>> 8) & 255;
                out[j * 4 + 3] = hs[j] & 255;
            }

            return out;
        },

        // --------------------------------------------------------------------
        // BigInt helpers
        // --------------------------------------------------------------------

        bytesToBigInt: function (bytes) {
            var hex = "";
            for (var i = 0; i < bytes.length; i++) {
                hex += (bytes[i] < 16 ? "0" : "") + bytes[i].toString(16);
            }
            return hex.length ? BigInt("0x" + hex) : BigInt(0);
        },

        bigIntToBytes: function (value, length) {
            var out = new Uint8Array(length);
            var v = value;
            var mask = BigInt(255);
            var eight = BigInt(8);
            for (var i = length - 1; i >= 0; i--) {
                out[i] = Number(v & mask);
                v >>= eight;
            }
            return out;
        },

        powMod: function (base, exp, mod) {
            var zero = BigInt(0), one = BigInt(1);
            var result = one;
            var b = base % mod;
            var e = exp;
            while (e > zero) {
                if (e & one) result = (result * b) % mod;
                b = (b * b) % mod;
                e >>= one;
            }
            return result;
        },

        // --------------------------------------------------------------------
        // Minimal DER reader + RSA private key parser (PKCS#8 and PKCS#1)
        // --------------------------------------------------------------------

        derRead: function (bytes, pos) {
            var tag = bytes[pos++];
            var len = bytes[pos++];
            if (len & 0x80) {
                var count = len & 0x7f;
                len = 0;
                for (var i = 0; i < count; i++) len = len * 256 + bytes[pos++];
            }
            return { tag: tag, start: pos, end: pos + len, next: pos + len };
        },

        parseRsaPrivateKey: function (der) {

            var self = GSheetsHelper;

            var seq = self.derRead(der, 0);
            if (seq.tag !== 0x30) throw new Error("Invalid private key: outer SEQUENCE missing.");

            var pos = seq.start;

            var version = self.derRead(der, pos);
            if (version.tag !== 0x02) throw new Error("Invalid private key: version missing.");
            pos = version.next;

            var second = self.derRead(der, pos);

            // PKCS#8: version, AlgorithmIdentifier(SEQUENCE), privateKey(OCTET STRING)
            if (second.tag === 0x30) {
                var octet = self.derRead(der, second.next);
                if (octet.tag !== 0x04) {
                    throw new Error("Invalid PKCS#8: OCTET STRING missing (is this an EC / unsupported key?).");
                }
                return self.parseRsaPrivateKey(der.subarray(octet.start, octet.end));
            }

            // PKCS#1: version, n, e, d, p, q, dp, dq, qInv
            var ints = [];
            var p2 = seq.start;

            while (p2 < seq.end && ints.length < 9) {
                var tlv = self.derRead(der, p2);
                if (tlv.tag !== 0x02) break;
                ints.push(self.bytesToBigInt(der.subarray(tlv.start, tlv.end)));
                p2 = tlv.next;
            }

            if (ints.length < 9) {
                throw new Error("Invalid RSA private key: expected 9 integers, found " + ints.length);
            }

            return {
                n: ints[1], e: ints[2], d: ints[3],
                p: ints[4], q: ints[5],
                dp: ints[6], dq: ints[7], qInv: ints[8]
            };
        },

        // --------------------------------------------------------------------
        // RS256 signature, pure JS (used when crypto.subtle is absent -> http)
        // --------------------------------------------------------------------

        signRs256Fallback: function (der, messageBytes) {

            var self = GSheetsHelper;

            if (typeof BigInt === "undefined") {
                throw new Error("BigInt is unavailable in this browser; cannot sign the JWT without crypto.subtle. Serve the build over HTTPS instead.");
            }

            var key = self.parseRsaPrivateKey(der);

            var modBytes = 0;
            var t = key.n;
            var eight = BigInt(8);
            var zero = BigInt(0);
            while (t > zero) { modBytes++; t >>= eight; }

            var digest = self.sha256(messageBytes);

            // DigestInfo prefix for SHA-256 (RFC 8017, 9.2)
            var prefix = [0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48,
                0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20];

            var tLen = prefix.length + digest.length;
            if (modBytes < tLen + 11) throw new Error("RSA modulus is too small.");

            // EMSA-PKCS1-v1_5: 0x00 0x01 FF..FF 0x00 || DigestInfo || hash
            var em = new Uint8Array(modBytes);
            em[0] = 0x00;
            em[1] = 0x01;
            for (var i = 2; i < modBytes - tLen - 1; i++) em[i] = 0xff;
            em[modBytes - tLen - 1] = 0x00;
            em.set(prefix, modBytes - tLen);
            em.set(digest, modBytes - digest.length);

            var m = self.bytesToBigInt(em);
            var sig;

            // CRT is ~4x faster than a plain d exponentiation.
            if (key.p && key.q && key.dp && key.dq && key.qInv) {
                var m1 = self.powMod(m, key.dp, key.p);
                var m2 = self.powMod(m, key.dq, key.q);
                var diff = ((m1 - m2) % key.p + key.p) % key.p;
                var h = (key.qInv * diff) % key.p;
                sig = m2 + h * key.q;
            }
            else {
                sig = self.powMod(m, key.d, key.n);
            }

            return self.bigIntToBytes(sig, modBytes);
        },

        // --------------------------------------------------------------------
        // RS256 signature via Web Crypto (https / localhost fast path)
        // --------------------------------------------------------------------

        signRs256Subtle: function (der, messageBytes) {

            // importKey needs a standalone ArrayBuffer copy.
            var buffer = new Uint8Array(der.length);
            buffer.set(der);

            return crypto.subtle.importKey(
                "pkcs8",
                buffer.buffer,
                { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
                false,
                ["sign"])
                .then(function (key) {
                    return crypto.subtle.sign(
                        { name: "RSASSA-PKCS1-v1_5" }, key, messageBytes);
                })
                .then(function (signature) {
                    return new Uint8Array(signature);
                });
        },

        // --------------------------------------------------------------------
        // Access token resolution (see header for the 4 supported shapes)
        // --------------------------------------------------------------------

        getAccessToken: function (authString) {

            var self = GSheetsHelper;

            if (self._token && Date.now() < self._tokenExpiry - 60000) {
                return Promise.resolve(self._token);
            }

            return (async function () {

                var config = null;
                var raw = (authString || "").trim();

                if (!raw) throw new Error("Auth string is empty.");

                if (raw.charAt(0) === "{") {
                    try {
                        config = JSON.parse(raw);
                    }
                    catch (parseError) {
                        throw new Error("Auth JSON is malformed: " +
                            (parseError && parseError.message ? parseError.message : parseError));
                    }
                }
                else {
                    // Bare access token
                    config = { access_token: raw };
                }

                // -------- proxy mode: no token at all --------
                if (config.proxy_url) {
                    self._proxyUrl = config.proxy_url;
                    self._token = null;
                    self._tokenExpiry = 0;
                    return null;
                }

                self._proxyUrl = null;

                // -------- pre-supplied token --------
                if (config.access_token) {
                    self._token = config.access_token;
                    self._tokenExpiry = Date.now() +
                        ((config.expires_in ? Number(config.expires_in) : 3600) * 1000);
                    console.log("[GSheets] Using supplied access token.");
                    return self._token;
                }

                // -------- token endpoint on your own backend --------
                if (config.token_url) {

                    console.log("[GSheets] Fetching access token from token_url...");

                    var tokenEndpointResponse = await fetch(config.token_url, {
                        method: config.token_method || "GET",
                        headers: config.headers || {},
                        body: config.token_body || undefined
                    });

                    var endpointBody = await tokenEndpointResponse.text();

                    if (!tokenEndpointResponse.ok) {
                        throw new Error("token_url HTTP " + tokenEndpointResponse.status +
                            ": " + endpointBody);
                    }

                    var endpointJson;

                    try {
                        endpointJson = JSON.parse(endpointBody);
                    }
                    catch (error) {
                        throw new Error("token_url returned non-JSON: " + endpointBody);
                    }

                    if (!endpointJson.access_token) {
                        throw new Error("token_url response has no access_token.");
                    }

                    self._token = endpointJson.access_token;
                    self._tokenExpiry = Date.now() +
                        ((endpointJson.expires_in || 3600) * 1000);

                    return self._token;
                }

                // -------- service account: sign a JWT locally --------
                if (!config.client_email || !config.private_key) {
                    throw new Error("Auth JSON must contain either access_token, token_url, proxy_url, or client_email + private_key.");
                }

                var secure = (typeof crypto !== "undefined") && !!crypto.subtle;

                console.log("[GSheets] Signing service-account JWT (" +
                    (secure ? "Web Crypto" : "pure-JS fallback, insecure context") + ")...");

                var now = Math.floor(Date.now() / 1000);

                var header = { alg: "RS256", typ: "JWT" };

                var payload = {
                    iss: config.client_email,
                    scope: config.scope ||
                        "https://www.googleapis.com/auth/spreadsheets.readonly",
                    aud: config.token_uri || "https://oauth2.googleapis.com/token",
                    iat: now,
                    exp: now + 3600
                };

                var unsigned =
                    self.base64UrlFromString(JSON.stringify(header)) + "." +
                    self.base64UrlFromString(JSON.stringify(payload));

                var der = self.pemToDer(config.private_key);
                var messageBytes = self.utf8Bytes(unsigned);

                var signatureBytes;
                var startTime = Date.now();

                if (secure) {
                    try {
                        signatureBytes = await self.signRs256Subtle(der, messageBytes);
                    }
                    catch (subtleError) {
                        console.warn("[GSheets] Web Crypto failed, falling back to pure JS.",
                            subtleError);
                        signatureBytes = self.signRs256Fallback(der, messageBytes);
                    }
                }
                else {
                    signatureBytes = self.signRs256Fallback(der, messageBytes);
                }

                console.log("[GSheets] JWT signed in " + (Date.now() - startTime) + "ms.");

                var jwt = unsigned + "." + self.base64UrlFromBytes(signatureBytes);

                var tokenResponse = await fetch(payload.aud, {
                    method: "POST",
                    headers: { "Content-Type": "application/x-www-form-urlencoded" },
                    body: "grant_type=" +
                        encodeURIComponent("urn:ietf:params:oauth:grant-type:jwt-bearer") +
                        "&assertion=" + encodeURIComponent(jwt)
                });

                var tokenBody = await tokenResponse.text();

                if (!tokenResponse.ok) {
                    throw new Error("Google OAuth HTTP " + tokenResponse.status +
                        ": " + tokenBody);
                }

                var tokenJson;

                try {
                    tokenJson = JSON.parse(tokenBody);
                }
                catch (error) {
                    throw new Error("Invalid OAuth response: " + tokenBody);
                }

                if (!tokenJson.access_token) {
                    throw new Error("OAuth response has no access_token: " + tokenBody);
                }

                self._token = tokenJson.access_token;
                self._tokenExpiry = Date.now() +
                    ((tokenJson.expires_in || 3600) * 1000);

                console.log("[GSheets] Access token acquired.");

                return self._token;
            })();
        },

        // --------------------------------------------------------------------
        // One place that performs every Sheets GET (direct or via proxy)
        // --------------------------------------------------------------------

        sheetsGet: function (pathAndQuery, authString) {

            var self = GSheetsHelper;

            return self.getAccessToken(authString).then(function (token) {

                var url;
                var options = { method: "GET" };

                if (self._proxyUrl) {
                    url = self._proxyUrl +
                        (self._proxyUrl.indexOf("?") >= 0 ? "&" : "?") +
                        "path=" + encodeURIComponent(pathAndQuery);
                }
                else {
                    url = "https://sheets.googleapis.com/v4/" + pathAndQuery;
                    options.headers = { "Authorization": "Bearer " + token };
                }

                console.log("[GSheets] GET " + url);

                return fetch(url, options);
            })
                .then(function (response) {
                    return response.text().then(function (body) {

                        console.log("[GSheets] HTTP " + response.status);

                        if (!response.ok) {
                            // 401/403 -> token may be stale; drop the cache so the
                            // next call re-authenticates.
                            if (response.status === 401 || response.status === 403) {
                                GSheetsHelper._token = null;
                                GSheetsHelper._tokenExpiry = 0;
                            }
                            throw new Error("Google Sheets HTTP " + response.status + ": " + body);
                        }

                        try {
                            return JSON.parse(body);
                        }
                        catch (error) {
                            throw new Error("Google Sheets returned non-JSON: " +
                                body.substring(0, 300));
                        }
                    });
                });
        }
    },

    // ========================================================================
    // GoogleSheets_GetSpreadsheet
    // ========================================================================

    GoogleSheets_GetSpreadsheet__deps: ['$GSheetsHelper'],
    GoogleSheets_GetSpreadsheet: function (spreadsheetIdPtr,
                                           gameObjectPtr,
                                           serviceAccountJsonPtr,
                                           requestIdPtr) {

        var spreadsheetId  = UTF8ToString(spreadsheetIdPtr);
        var gameObjectName = UTF8ToString(gameObjectPtr);
        var authString     = UTF8ToString(serviceAccountJsonPtr);
        var requestId      = UTF8ToString(requestIdPtr);

        console.log("[GSheets] GetSpreadsheet START | id=" + spreadsheetId +
            " | go=" + gameObjectName + " | req=" + requestId +
            " | secureContext=" +
            ((typeof crypto !== "undefined" && !!crypto.subtle) ? "yes" : "no"));

        var path = "spreadsheets/" + encodeURIComponent(spreadsheetId) +
            "?fields=" + encodeURIComponent(
                "sheets(properties(title,gridProperties(rowCount,columnCount)))");

        GSheetsHelper.sheetsGet(path, authString)
            .then(function (data) {
                var count = (data && data.sheets) ? data.sheets.length : 0;
                console.log("[GSheets] GetSpreadsheet OK | sheets=" + count);
                GSheetsHelper.sendResponse(gameObjectName, requestId, true, "", data);
            })
            .catch(function (error) {
                var message = error && error.message ? error.message : String(error);
                console.error("[GSheets] GetSpreadsheet ERROR", error);
                GSheetsHelper.sendResponse(gameObjectName, requestId, false, message, null);
            });
    },

    // ========================================================================
    // GoogleSheets_GetValuesBatch
    // ========================================================================

    GoogleSheets_GetValuesBatch__deps: ['$GSheetsHelper'],
    GoogleSheets_GetValuesBatch: function (spreadsheetIdPtr,
                                           gameObjectPtr,
                                           serviceAccountJsonPtr,
                                           rangesJsonPtr,
                                           requestIdPtr) {

        var spreadsheetId  = UTF8ToString(spreadsheetIdPtr);
        var gameObjectName = UTF8ToString(gameObjectPtr);
        var authString     = UTF8ToString(serviceAccountJsonPtr);
        var rangesJson     = UTF8ToString(rangesJsonPtr);
        var requestId      = UTF8ToString(requestIdPtr);

        console.log("[GSheets] GetValuesBatch START | id=" + spreadsheetId +
            " | req=" + requestId);

        var ranges;

        try {
            var parsed = JSON.parse(rangesJson);
            ranges = parsed && parsed.values;

            if (!Array.isArray(ranges) || ranges.length === 0) {
                throw new Error("No ranges provided.");
            }
        }
        catch (error) {
            var parseMessage = "Invalid ranges JSON (" +
                (error && error.message ? error.message : error) + "): " + rangesJson;

            console.error("[GSheets] " + parseMessage);

            GSheetsHelper.sendResponse(gameObjectName, requestId, false, parseMessage, null);

            return;
        }

        console.log("[GSheets] RangeCount=" + ranges.length);

        // Google caps a batchGet URL; chunk to stay well inside limits.
        var CHUNK = 25;
        var chunks = [];

        for (var i = 0; i < ranges.length; i += CHUNK) {
            chunks.push(ranges.slice(i, i + CHUNK));
        }

        var basePath = "spreadsheets/" + encodeURIComponent(spreadsheetId) +
            "/values:batchGet?majorDimension=ROWS";

        // Chunks run sequentially: the first call warms the token cache, so the
        // JWT is signed only once.
        var collected = [];

        function runChunk(index) {

            if (index >= chunks.length) {
                var merged = {
                    spreadsheetId: spreadsheetId,
                    valueRanges: collected
                };

                console.log("[GSheets] GetValuesBatch OK | valueRanges=" + collected.length);

                GSheetsHelper.sendResponse(gameObjectName, requestId, true, "", merged);

                return;
            }

            var path = basePath;

            for (var j = 0; j < chunks[index].length; j++) {
                path += "&ranges=" + encodeURIComponent(chunks[index][j]);
            }

            GSheetsHelper.sheetsGet(path, authString)
                .then(function (data) {

                    if (data && data.valueRanges) {
                        for (var k = 0; k < data.valueRanges.length; k++) {
                            collected.push(data.valueRanges[k]);
                        }
                    }

                    runChunk(index + 1);
                })
                .catch(function (error) {
                    var message = error && error.message ? error.message : String(error);
                    console.error("[GSheets] GetValuesBatch ERROR", error);
                    GSheetsHelper.sendResponse(gameObjectName, requestId, false, message, null);
                });
        }

        runChunk(0);
    }
};

mergeInto(LibraryManager.library, GoogleSheetsLibrary);
