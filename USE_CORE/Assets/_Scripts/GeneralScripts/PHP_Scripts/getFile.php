<?php

// Retrieve the full path from the query parameters
$fullPath = $_GET['path'];

// Validate the path
if (empty($fullPath)) {
    echo "Invalid parameters";
    return;
}

// Extract the directory path and file name from the full path
$lastSlashPos = strrpos($fullPath, '/');
if ($lastSlashPos === false) {
    echo "Invalid full path";
    return;
}

$directoryPath = substr($fullPath, 0, $lastSlashPos);
$searchString = substr($fullPath, $lastSlashPos + 1);

// Get the file names in the specified directory
$fileNames = scandir($directoryPath);

// Search for the specified file name
$searchResult = null;
foreach ($fileNames as $fileName) {
    if ($fileName === $searchString) {
        $searchResult = $fileName;
        break;
    }
}

// Read the contents of the search result file
if ($searchResult !== null) {
    $filePath = $directoryPath . '/' . $searchResult;
    $fileContents = file_get_contents($filePath);

    // Echo the file name
    echo $searchResult;

    echo "\n##########\n";

    // Echo the contents
    echo $fileContents;
} else {
    echo "File not found";
}

?>