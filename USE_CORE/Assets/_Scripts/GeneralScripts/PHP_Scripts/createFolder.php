<?php
require_once 'security.php';

$folderPath = $_REQUEST['path'] ?? '';

if (empty($folderPath)) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Missing folder path']);
    exit;
}

// Sanitize path
$folderPath = safe_path($folderPath);

// Create folder
if (!file_exists($folderPath)) {
    try {
        mkdir($folderPath, 0755, true);
        echo json_encode(['ok' => true, 'message' => 'Folder created successfully']);
    } catch (Exception $e) {
        http_response_code(500);
        echo json_encode(['ok' => false, 'error' => 'Failed to create folder']);
    }
} else {
    echo json_encode(['ok' => true, 'message' => 'Folder already exists']);
}
?>
