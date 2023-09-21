<?php

$response = array(
    "status" => "success",
    "message" => "Server is reachable!",
);

// Set the content type to JSON
header('Content-Type: application/json');

// Output the response as JSON
echo json_encode($response);
?>