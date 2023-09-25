<?php
$path = $_GET['path'];
$data = $_POST['data'];

if (!empty($path)) {
    // Update the file contents
    if (file_exists($path)) {
        file_put_contents($path, $data);
        echo "File updated successfully.";
    } else {
        echo "File does not exist.";
    }
} else {
    echo "Invalid path specified.";
}
?>