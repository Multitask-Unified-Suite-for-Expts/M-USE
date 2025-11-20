<?php
require_once 'security.php';

function searchFileInFolder($folderPath, $searchString) {
    // Sanitize folder path
    $folderPath = safe_path($folderPath);

    // Get all files and folders in the folder
    $files = glob($folderPath . '/*');

    // First, check files in the current directory
    foreach ($files as $file) {
        if (is_file($file)) {
            $fileName = pathinfo($file, PATHINFO_BASENAME);
            if (strpos($fileName, $searchString) !== false) {
                return $file;
            }
        }
    }

    // Recursively search subdirectories
    foreach ($files as $file) {
        if (is_dir($file)) {
            $subFilePath = searchFileInFolder($file, $searchString);
            if ($subFilePath !== null) {
                return $subFilePath;
            }
        }
    }

    return null;
}

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    $folderPath = $_GET['folderPath'] ?? '';
    $searchString = $_GET['searchString'] ?? '';

    if (empty($folderPath) || empty($searchString)) {
        http_response_code(400);
        echo "File not found";
        exit;
    }

    $folderPath = safe_path($folderPath);
    $filePath = searchFileInFolder($folderPath, $searchString);

    echo $filePath !== null ? $filePath : "File not found";
}
?>
