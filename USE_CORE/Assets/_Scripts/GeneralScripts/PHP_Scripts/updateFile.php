<?php

require_once 'security.php';

// Accept GET or POST parameters
$path = $_GET['path'] ?? '';
$data = $_POST['data'] ?? '';

if (empty($path)) {
    http_response_code(400);
    echo "Invalid path specified.";
    exit;
}

// Sanitize path
$path = safe_path($path);

// Check if file exists
if (!file_exists($path)) {
    http_response_code(404);
    echo "File does not exist.";
    exit;
}

// Update the file
if (file_put_contents($path, $data) !== false) {
    echo "File updated successfully.";
} else {
    http_response_code(500);
    echo "Failed to update file.";
}
?>
