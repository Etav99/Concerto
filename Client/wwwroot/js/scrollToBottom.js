﻿function scrollToBottom(containerId) {
    container = document.getElementById(containerId);
    console.log(container)


    if (container) {
        container.scrollIntoView(false);
    }
        
}