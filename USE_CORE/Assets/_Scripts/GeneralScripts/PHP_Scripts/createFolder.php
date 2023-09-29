<?php
$path = $_POST['path'];

if (!empty($path)) {
    // Create the folder
    if (!file_exists($path)) {
        mkdir($path, 0777, true);
        echo "Folder created successfully.";
    } else {
        echo "Folder already exists.";
    }
} else {
    echo "Invalid path specified.";
}
?>