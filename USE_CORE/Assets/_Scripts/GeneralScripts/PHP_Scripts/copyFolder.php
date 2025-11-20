<?php
require_once 'security.php';

// Helper: recursively copy a folder within the safe base directory
function copyFolder($src, $dst) {
    if (!is_dir($src)) return false;

    if (!file_exists($dst)) mkdir($dst, 0755, true);

    $dir = opendir($src);
    if (!$dir) return false;

    while (($file = readdir($dir)) !== false) {
        if ($file === '.' || $file === '..') continue;

        $srcFile = $src . '/' . $file;
        $dstFile = $dst . '/' . $file;

        if (is_dir($srcFile)) {
            if (!copyFolder($srcFile, $dstFile)) {
                closedir($dir);
                return false;
            }
        } else {
            if (!copy($srcFile, $dstFile)) {
                closedir($dir);
                return false;
            }
        }
    }

    closedir($dir);
    return true;
}

// Accept either GET or POST
$sourcePath = $_REQUEST['sourcePath'] ?? '';
$destinationPath = $_REQUEST['destinationPath'] ?? '';

// Validate input
if (empty($sourcePath) || empty($destinationPath)) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Missing source or destination path']);
    exit;
}

// Sanitize paths
$src = safe_path($sourcePath);
$dst = safe_path($destinationPath);

// Perform copy
if (copyFolder($src, $dst)) {
    echo json_encode(['ok' => true, 'message' => 'Folder copied successfully']);
} else {
    http_response_code(500);
    echo json_encode(['ok' => false, 'error' => 'Failed to copy folder']);
}
?>
