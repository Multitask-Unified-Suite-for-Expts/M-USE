<?php
require_once 'security.php';

$path = $_REQUEST['path'] ?? '';

if (empty($path)) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Missing path']);
    exit;
}

// Sanitize path
$path = safe_path($path);

// Read file content from request body
$content = file_get_contents('php://input');
if ($content === false) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Error reading request body']);
    exit;
}

// Write file
try {
    file_put_contents($path, $content);
    echo json_encode(['ok' => true, 'message' => 'File created/replaced successfully']);
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['ok' => false, 'error' => 'Failed to write file']);
}
?>
