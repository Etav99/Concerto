function downloadFile (fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

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


function enablePreventWindowClose(requestId) {

    // create requestId hashset if not exists
    if(!window.preventWindowCloseActiveRequests)
        window.preventWindowCloseActiveRequests = new Set();

    // add request id
    window.preventWindowCloseActiveRequests.add(requestId);
    window.onbeforeunload = function () { return ""; }
}

function disablePreventWindowClose(requestId) {

    if (!window.preventWindowCloseActiveRequests)
        return;

    // remove request id
    window.preventWindowCloseActiveRequests.delete(requestId);

    // if no more requests, remove event listener
    if (window.preventWindowCloseActiveRequests.size == 0) {
        window.onbeforeunload = null;
    }
}

function isIos()
{
    return /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
}