<?php

require_once 'security.php';

$response = array(
    "status" => "success",
    "message" => "Server is reachable!",
);

// Set the content type to JSON
header('Content-Type: application/json');

// Output the response
echo json_encode($response);
?>
