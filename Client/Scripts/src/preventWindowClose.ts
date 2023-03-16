export function enablePreventWindowClose(requestId: string) {

    // create requestId set if not exists
    if(!window.preventWindowCloseActiveRequests)
        window.preventWindowCloseActiveRequests = new Set();

    // add request id
    window.preventWindowCloseActiveRequests.add(requestId);
    window.onbeforeunload = function () { return ""; }
}

export function disablePreventWindowClose(requestId: string) {
    if (!window.preventWindowCloseActiveRequests)
        return;

    // remove request id
    window.preventWindowCloseActiveRequests.delete(requestId);

    // if no more requests, remove event listener
    if (window.preventWindowCloseActiveRequests.size == 0) {
        window.onbeforeunload = null;
    }
}