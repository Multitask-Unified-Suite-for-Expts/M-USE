<?php
function searchFileInFolder($folderPath, $searchString) {
    // Get all files in the folder
    $files = glob($folderPath . '/*');

    // First, check files in the current directory
    foreach ($files as $file) {
        if (is_file($file)) {
            // Extract the file name from the path
            $fileName = pathinfo($file, PATHINFO_BASENAME);

            // Check if the file name contains the searchString
            if (strpos($fileName, $searchString) !== false) {
                // Found the file matching the searchString
                return $file;
            }
        }
    }

    // If the file is not found in the current directory, recursively search subdirectories
    foreach ($files as $file) {
        if (is_dir($file)) {
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