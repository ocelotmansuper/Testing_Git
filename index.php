<?php
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST');
header('Access-Control-Allow-Headers: Content-Type');

// Подключение к БД
$connection = new mysqli("localhost", "u3163899_leader_user", "Renatik179!", "u3163899_leaderboard");
$connection->set_charset("utf8mb4");

// Функция для скачивания и сохранения аватара
function downloadAndSaveAvatar($url, $vkId) {
    $avatarsDir = __DIR__ . '/avatars/';
    if (!file_exists($avatarsDir)) {
        mkdir($avatarsDir, 0755, true);
    }

    $fileName = $avatarsDir . $vkId . '.jpg';
    $savedUrl = 'https://misterimrt.online/api/avatars/' . $vkId . '.jpg';

    if (file_exists($fileName) && (time() - filemtime($fileName) < 86400)) {
        return $savedUrl;
    }

    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
    curl_setopt($ch, CURLOPT_USERAGENT, 'Mozilla/5.0');
    $data = curl_exec($ch);
    $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    if ($httpCode === 200) {
        file_put_contents($fileName, $data);
        return $savedUrl;
    }

    return 'https://misterimrt.online/api/avatars/default.jpg';
}

// Функция проверки JSON
function isValidJson($str) {
    if (empty($str)) return false;
    if (!is_string($str)) return false;
    try {
        json_decode($str);
        return json_last_error() === JSON_ERROR_NONE;
    } catch(Exception $e) {
        return false;
    }
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $rawData = file_get_contents('php://input');
    $data = json_decode($rawData, true);
    
    if (!$data || !isset($data['vk_id'])) {
        echo json_encode(['success' => false, 'error' => 'Invalid data']);
        exit;
    }

    // Проверяем, существует ли пользователь
    $checkStmt = $connection->prepare("SELECT vk_id FROM leaderboard WHERE vk_id = ?");
    $checkStmt->bind_param("s", $data['vk_id']);
    $checkStmt->execute();
    $checkResult = $checkStmt->get_result();
    $exists = $checkResult->num_rows > 0;
    $checkStmt->close();

    if (!$exists) {
        // Создаем нового пользователя
        $stmt = $connection->prepare("\n            INSERT INTO leaderboard (vk_id, name, photo_url, score, upgrades, last_online) 
            VALUES (?, ?, ?, ?, ?, ?)
        ");

        $score = isset($data['score']) ? intval($data['score']) : 0;
        $upgrades = isset($data['upgrades']) && isValidJson($data['upgrades']) ? $data['upgrades'] : '[]';
        $lastOnline = isset($data['last_online']) ? intval($data['last_online']) : time();
        $name = isset($data['name']) ? $data['name'] : '';
        $photoUrl = isset($data['photo_url']) ? $data['photo_url'] : '';

        $stmt->bind_param("sssisi", 
            $data['vk_id'],
            $name,
            $photoUrl,
            $score,
            $upgrades,
            $lastOnline
        );
    } else {
        // Обновляем существующего пользователя
        $updateFields = [];
        $params = [];
        $types = '';

        if (isset($data['score'])) {
            $updateFields[] = "score = ?";
            $params[] = intval($data['score']);
            $types .= 'i';
        }

        if (isset($data['upgrades'])) {
            $rawUpgrades = $data['upgrades'];
            $isValid = isValidJson($rawUpgrades);

            if ($isValid && $rawUpgrades !== '') {
                $updateFields[] = "upgrades = ?";
                $params[] = $rawUpgrades;
                $types .= 's';
            } else {
                // Не сбрасывать upgrades, если данные некорректны или пусты
                error_log("Invalid or empty upgrades received for vk_id: {$data['vk_id']}");
            }
        }

        if (isset($data['last_online'])) {
            $updateFields[] = "last_online = ?";
            $params[] = intval($data['last_online']);
            $types .= 'i';
        }

        if (isset($data['name'])) {
            $updateFields[] = "name = ?";
            $params[] = $data['name'];
            $types .= 's';
        }

        if (isset($data['photo_url'])) {
            $updateFields[] = "photo_url = ?";
            $params[] = $data['photo_url'];
            $types .= 's';
        }

        $params[] = $data['vk_id'];
        $types .= 's';

        $sql = "UPDATE leaderboard SET " . implode(", ", $updateFields) . " WHERE vk_id = ?";
        $stmt = $connection->prepare($sql);
        $stmt->bind_param($types, ...$params);
    }

    try {
        if ($stmt->execute()) {
            echo json_encode(['success' => true]);
        } else {
            throw new Exception($stmt->error);
        }
    } catch (Exception $e) {
        echo json_encode(['success' => false, 'error' => $e->getMessage()]);
    }

    $stmt->close();
} else if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    if (isset($_GET['vk_id'])) {
        // Получение данных конкретного игрока
        $stmt = $connection->prepare("\n            SELECT 
                vk_id, 
                name, 
                photo_url, 
                score, 
                COALESCE(upgrades, '[]') as upgrades,
                COALESCE(last_online, 0) as last_online 
            FROM leaderboard 
            WHERE vk_id = ? 
            LIMIT 1
        ");

        $stmt->bind_param("s", $_GET['vk_id']);
        $stmt->execute();
        $result = $stmt->get_result();
        $player = $result->fetch_assoc();

        echo json_encode([
            'success' => true,
            'data' => $player ? [$player] : []
        ]);

        $stmt->close();
    } else {
        // Получение топ 10 игроков
        $result = $connection->query("\n            SELECT vk_id, name, photo_url, score 
            FROM leaderboard 
            ORDER BY score DESC 
            LIMIT 10
        ");

        $leaders = [];
        while ($row = $result->fetch_assoc()) {
            $leaders[] = $row;
        }

        echo json_encode([
            'success' => true,
            'data' => $leaders
        ]);
    }
}

$connection->close();
?>