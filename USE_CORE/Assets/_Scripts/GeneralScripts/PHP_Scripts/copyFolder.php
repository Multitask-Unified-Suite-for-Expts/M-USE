<?php

function copyFolder($source, $destination) {
    if (!is_dir($source)) {
        return false;
    }

    if (!is_dir($destination)) {
        mkdir($destination, 0777, true);
    }

    $dirHandle = opendir($source);

    while (($file = readdir($dirHandle)) !== false) {
        if ($file != "." && $file != "..") {
            $sourceFile = $source . '/' . $file;
            $destinationFile = $destination . '/' . $file;

            if (is_dir($sourceFile)) {
                copyFolder($sourceFile, $destinationFile);
            } else {
                copy($sourceFile, $destinationFile);
            }
        }
    }

    closedir($dirHandle);
    return true;
}

$sourcePath = $_GET['sourcePath'];
$destinationPath = $_GET['destinationPath'];

// Validate the paths
if (empty($sourcePath) || empty($destinationPath)) {
    echo "Invalid paths";
    return;
}

// Copy the folder and its contents
$result = copyFolder($sourcePath, $destinationPath);

if ($result) {
    echo "Folder copied successfully";
} else {
    echo "Failed to copy folder";
}

?>
