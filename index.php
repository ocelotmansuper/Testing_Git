<?php
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST');
header('Access-Control-Allow-Headers: Content-Type');

// Подключение к базе данных
$connection = new mysqli("localhost", "u3163899_leader_user", "Renatik179!", "u3163899_leaderboard");
$connection->set_charset("utf8mb4");

function isValidJson($str) {
    if (empty($str)) return false;
    if (!is_string($str)) return false;
    try {
        json_decode($str);
        return json_last_error() === JSON_ERROR_NONE;
    } catch (Exception $e) {
        return false;
    }
}

// Сохранение или обновление данных костюмов
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_GET['action']) && $_GET['action'] === 'player_costumes') {
    $rawData = file_get_contents('php://input');
    $data = json_decode($rawData, true);

    // Логируем POST-запрос для отладки
    error_log("[POST Request] Raw Data: " . $rawData);
    error_log("[POST Request] Parsed Data: " . json_encode($data, JSON_PRETTY_PRINT));

    if (!$data || !isset($data['vk_id'])) {
        echo json_encode(['success' => false, 'error' => 'Invalid or missing VK ID']);
        exit;
    }

    $vk_id = $data['vk_id'];

    // Проверяем существование пользователя и сохраняем его текущие данные
    $stmt = $connection->prepare("SELECT purchased_costumes, last_equipped_costume FROM leaderboard WHERE vk_id = ?");
    $stmt->bind_param("s", $vk_id);
    $stmt->execute();
    $result = $stmt->get_result();
    $existingData = $result->fetch_assoc();
    $stmt->close();

    // Используем существующие данные, если новые не переданы
    $purchasedCostumes = isset($data['purchasedCostumes']) ? json_encode($data['purchasedCostumes'], JSON_UNESCAPED_UNICODE) : $existingData['purchased_costumes'];
    $lastEquipped = isset($data['lastEquipped']) ? $data['lastEquipped'] : $existingData['last_equipped_costume'];

    // Логируем данные перед записью в базу
    error_log("[Updated Data for vk_id=$vk_id]: purchasedCostumes = $purchasedCostumes, lastEquipped = $lastEquipped");

    $stmt = $connection->prepare("UPDATE leaderboard SET purchased_costumes = ?, last_equipped_costume = ? WHERE vk_id = ?");
    $stmt->bind_param("sss", $purchasedCostumes, $lastEquipped, $vk_id);
    $stmt->execute();
    $stmt->close();

    echo json_encode(['success' => true]);
}

// Загрузка данных костюмов
else if ($_SERVER['REQUEST_METHOD'] === 'GET' && isset($_GET['action']) && $_GET['action'] === 'player_costumes' && isset($_GET['vk_id'])) {
    $vk_id = $_GET['vk_id'];

    error_log("[GET Request for costumes - vk_id = {$vk_id}]");

    // Получаем данные костюмов
    $stmt = $connection->prepare("SELECT purchased_costumes, last_equipped_costume FROM leaderboard WHERE vk_id = ?");
    $stmt->bind_param("s", $vk_id);
    $stmt->execute();
    $result = $stmt->get_result();
    $costumeData = $result->fetch_assoc();
    $stmt->close();

    if ($costumeData) {
        error_log("[Costume Data retrieved successfully for vk_id={$vk_id}]: " . print_r($costumeData, true));
        echo json_encode([
            'success' => true,
            'data' => [
                'purchasedCostumes' => json_decode($costumeData['purchased_costumes']),
                'lastEquipped' => $costumeData['last_equipped_costume']
            ]
        ]);
    } else {
        error_log("[No Costume Data found for vk_id={$vk_id}]");
        echo json_encode(['success' => false, 'error' => 'Player not found']);
    }
}

// Сохранение или обновление остальных данных игрока
else if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $rawData = file_get_contents('php://input');
    $data = json_decode($rawData, true);
    
    if (!$data || !isset($data['vk_id'])) {
        echo json_encode(['success' => false, 'error' => 'Invalid data']);
        exit;
    }

    $checkStmt = $connection->prepare("SELECT vk_id FROM leaderboard WHERE vk_id = ?");
    $checkStmt->bind_param("s", $data['vk_id']);
    $checkStmt->execute();
    $checkResult = $checkStmt->get_result();
    $exists = $checkResult->num_rows > 0;
    $checkStmt->close();

    if (!$exists) {
        // Создаем нового пользователя
        $stmt = $connection->prepare("INSERT INTO leaderboard (vk_id, name, photo_url, score, upgrades, last_online) 
            VALUES (?, ?, ?, ?, ?, ?)");
        
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
}

// Загрузка данных игрока и топ лидеров
else if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    if (isset($_GET['vk_id'])) {
        $stmt = $connection->prepare("SELECT vk_id, name, photo_url, score, COALESCE(upgrades, '[]') as upgrades, COALESCE(last_online, 0) as last_online FROM leaderboard WHERE vk_id = ?");
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
        $result = $connection->query("SELECT vk_id, name, photo_url, score FROM leaderboard ORDER BY score DESC LIMIT 10");
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