function scrollToBottom(containerId) {
    container = document.getElementById(containerId);

    if (container) {
        container.scrollTop = container.scrollHeight;
    }
        
}


function scrollToElement(elementId, containerId) {
    container = document.getElementById(containerId);



    if (container) {
        container.scrollTop = container.scrollHeight;
    }

}