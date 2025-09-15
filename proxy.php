<?php
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET');

if (isset($_GET['url'])) {
    $url = urldecode($_GET['url']);
    
    // Проверяем, что URL от VK
    if (strpos($url, 'userapi.com') !== false || 
        strpos($url, 'vk.com') !== false || 
        strpos($url, 'vk-cdn') !== false) {
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, 1);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, false);
        curl_setopt($ch, CURLOPT_USERAGENT, 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36');
        
        $result = curl_exec($ch);
        $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $content_type = curl_getinfo($ch, CURLINFO_CONTENT_TYPE);
        
        if (curl_errno($ch)) {
            error_log('Curl error: ' . curl_error($ch));
        }
        
        curl_close($ch);
        
        if ($http_code == 200) {
            header('Content-Type: ' . $content_type);
            echo $result;
        } else {
            error_log('Failed to load image. HTTP Code: ' . $http_code . ' URL: ' . $url);
            header('HTTP/1.1 404 Not Found');
        }
    } else {
        header('HTTP/1.1 403 Forbidden');
    }
} else {
    header('HTTP/1.1 400 Bad Request');
}
?>