<?php

$directoryPath = $_GET['directoryPath'];

// Ensure the directory path is valid and exists
if (is_dir($directoryPath)) {
    // Open the directory
    if ($dirHandle = opendir($directoryPath)) {
        $folderNames = '';

        // Read each entry in the directory
        while (($entry = readdir($dirHandle)) !== false) {
            // Exclude current directory and parent directory
            if ($entry != "." && $entry != "..") {
                // Check if the entry is a directory
                if (is_dir($directoryPath . '/' . $entry)) {
                    // Concatenate the folder name to the string
                    $folderNames .= $entry . ',';
                }
            }
        }

        // Close the directory handle
        closedir($dirHandle);

        // Output the folder names as plain text
        echo rtrim($folderNames, ',');
    } else {
        // Failed to open the directory
        echo "Failed to open the directory.";
    }
} else {
    // Invalid or non-existent directory path
    echo "Invalid or non-existent directory path.";
}

?>