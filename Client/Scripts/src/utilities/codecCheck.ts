const FILE_EXTENSIONS = ["mp4", "webm", "ogg", "x-matroska"];
const CODECS = ["vp9", "vp9.0", "vp8", "vp8.0", "avc1", "av1", "h265", "h.265", "h264", "h.264"];

export function getAnySupportedVideoMimeTypeAndExtension(): [string, string]   {
    for (const ext of FILE_EXTENSIONS) {
        for (const codec of CODECS) {
            var mimeType = `video/${ext};codecs=${codec},opus`;
            if (MediaRecorder.isTypeSupported(mimeType)) {
                return [mimeType, ext];
            }
        }
    }
    throw new Error("Browser doesn't support any of the codecs");
}

export function getAnySupportedAudioMimeTypeAndExtension(): [string, string]   {
    for (const ext of FILE_EXTENSIONS) {
        for (const codec of CODECS) {
            var mimeType = `audio/${ext};codecs=opus`;
            if (MediaRecorder.isTypeSupported(mimeType)) {
                return [mimeType, ext];
            }
        }
    }
    throw new Error("Browser doesn't support any of the codecs");
}