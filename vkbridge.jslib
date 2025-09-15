mergeInto(LibraryManager.library, {
  SendVKMessage: function(message) {
    var messageStr = UTF8ToString(message);
    try {
      var data = JSON.parse(messageStr);
      console.log("VK Bridge sending message:", data); // добавляем лог для отладки
      vkBridge.send(data.method, JSON.parse(data.parameters)) // исправляем params на parameters и парсим JSON
        .then(function(result) {
          console.log("VK Bridge result:", result); // добавляем лог для отладки
          if (window.unityInstance) {
            window.unityInstance.SendMessage('GameManager', 'OnVKUserInfoReceived', JSON.stringify(result));
          }
        })
        .catch(function(error) {
          console.error('VK Bridge error:', error);
          if (window.unityInstance) {
            window.unityInstance.SendMessage('GameManager', 'OnVKUserInfoReceived', 
              JSON.stringify({ error: error.message }));
          }
        });
    } catch (error) {
      console.error('Error parsing message:', error);
    }
  },

  IsMobileDevice: function() {
    // Check for mobile device using user agent and touch capabilities
    var isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
        (navigator.maxTouchPoints && navigator.maxTouchPoints > 2);
        
    // Detailed platform information for debugging
    console.log("[Platform Detection - JavaScript] Detailed Info:");
    console.log("- User Agent: " + navigator.userAgent);
    console.log("- Touch Points: " + navigator.maxTouchPoints);
    console.log("- Screen Size: " + window.innerWidth + "x" + window.innerHeight);
    console.log("- Platform Type: " + (isMobile ? "Mobile" : "Desktop"));
    
    return isMobile;
  }
});