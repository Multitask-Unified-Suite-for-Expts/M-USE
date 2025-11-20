<?php
require_once 'security.php';

$directoryPath = $_GET['directoryPath'] ?? '';

if (empty($directoryPath)) {
    http_response_code(400);
    echo "";
    exit;
}

// Sanitize the directory path
$directoryPath = safe_path($directoryPath);

// Ensure the directory exists
if (is_dir($directoryPath)) {
    $folderNames = '';
    if ($dirHandle = opendir($directoryPath)) {
        while (($entry = readdir($dirHandle)) !== false) {
            if ($entry !== "." && $entry !== "..") {
                if (is_dir($directoryPath . '/' . $entry)) {
                    $folderNames .= $entry . ',';
                }
            }
        }
        closedir($dirHandle);
        // Output folder names as a comma-separated string
        echo rtrim($folderNames, ',');
    } else {
        echo "Failed to open the directory.";
    }
} else {
    echo "Invalid or non-existent directory path.";
}
?>
