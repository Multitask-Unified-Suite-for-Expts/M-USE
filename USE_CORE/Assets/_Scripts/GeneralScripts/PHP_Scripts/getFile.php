<?php
require_once 'security.php';

// Accept GET or POST
$fullPath = $_REQUEST['path'] ?? '';

// Validate
if (empty($fullPath)) {
    http_response_code(400);
    echo "Invalid parameters";
    exit;
}

// Sanitize full path
$fullPath = safe_path($fullPath);

// Extract directory and file name
$lastSlashPos = strrpos($fullPath, '/');
if ($lastSlashPos === false) {
    echo "Invalid full path";
    exit;
}

$directoryPath = substr($fullPath, 0, $lastSlashPos);
$searchString = substr($fullPath, $lastSlashPos + 1);

// Get files in directory
$fileNames = scandir($directoryPath);
$searchResult = null;
foreach ($fileNames as $fileName) {
    if ($fileName === $searchString) {
        $searchResult = $fileName;
        break;
    }
}

if ($searchResult !== null) {
    $filePath = $directoryPath . '/' . $searchResult;
    $fileContents = file_get_contents($filePath);

    // Echo in same string format as before
    echo $searchResult . "\n##########\n" . $fileContents;
} else {
    echo "File not found";
}
?>
