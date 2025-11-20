<?php
/**
 * security.php
 * -----------------------
 * Unified security checks for Unity builds:
 * - WebGL: requires X-Unity-Client header
 * - Windows build & Unity Editor: bypass origin checks
 * Includes path sanitization via safe_path()
 */

// Fetch relevant headers
$origin = $_SERVER['HTTP_ORIGIN'] ?? '';
$client = $_SERVER['HTTP_X_UNITY_CLIENT'] ?? '';
$userAgent = $_SERVER['HTTP_USER_AGENT'] ?? '';

// Detect environments
$isEditor = php_sapi_name() === 'cli' || strpos($userAgent, 'Unity') !== false;
$isWindowsBuild = empty($origin);   // No Origin header, running locally
$isWebGL = !empty($origin);         // Origin header exists => running in browser

// WebGL security check
if ($isWebGL && $client !== 'MUSE-Experiment') {
    http_response_code(403);
    echo "Forbidden: Unity client header missing or invalid.";
    exit;
}

/**
 * Sanitize paths to prevent directory traversal.
 * Usage: $safePath = safe_path($userProvidedPath);
 */
function safe_path($path) {
    // Normalize slashes
    $path = str_replace('\\', '/', $path);
    // Remove any "../" or "..\" attempts
    $path = preg_replace('/\.\.+[\/\\\\]/', '', $path);
    // Optional: remove any trailing slashes
    $path = rtrim($path, '/');
    return $path;
}


?>
