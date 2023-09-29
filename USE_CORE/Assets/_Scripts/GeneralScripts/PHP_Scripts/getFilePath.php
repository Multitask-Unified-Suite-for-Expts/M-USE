<?php
function searchFileInFolder($folderPath, $searchString) {
    $files = glob($folderPath . '/*'); // Get all files in the folder
    foreach ($files as $file) {
        if (is_file($file) && strpos($file, $searchString) !== false) {
            // Found the file matching the searchString
            return $file;
        } elseif (is_dir($file)) {
            // Recursively search subdirectories
            $subFolderPath = $file;
            $subFilePath = searchFileInFolder($subFolderPath, $searchString);
            if ($subFilePath !== null) {
                return $subFilePath;
            }
        }
    }
    // File not found in this folder or its subdirectories
    return null;
}

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    $folderPath = $_GET['folderPath'];
    $searchString = $_GET['searchString'];

    $filePath = searchFileInFolder($folderPath, $searchString);

    if ($filePath !== null) {
        echo $filePath;
    } else {
        echo "File not found";
    }
}
?>